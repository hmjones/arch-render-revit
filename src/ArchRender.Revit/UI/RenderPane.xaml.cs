using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using ArchRender.Revit.Models;
using ArchRender.Revit.Services;
using Microsoft.Win32;

namespace ArchRender.Revit.UI;

public partial class RenderPane : Page, IDockablePaneProvider
{
    private string? _lastResultUrl;
    private CancellationTokenSource? _cts;
    private UIApplication? _uiApp;

    public RenderPane()
    {
        InitializeComponent();
        RenderTypeCombo.SelectionChanged += RenderTypeCombo_SelectionChanged;
        CheckApiKey();
    }

    // Called by Revit when it sets up the dockable pane
    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = this;
        data.InitialState = new DockablePaneState
        {
            DockPosition = DockPosition.Right,
            MinimumWidth = 280,
        };
    }

    // Called by Revit to inject the UIApplication so we can access the document
    internal void SetApplication(UIApplication uiApp) => _uiApp = uiApp;

    private void CheckApiKey()
    {
        var key = CredentialStore.LoadApiKey();
        NoKeyBanner.Visibility = string.IsNullOrWhiteSpace(key)
            ? Visibility.Visible
            : Visibility.Collapsed;
        RenderButton.IsEnabled = !string.IsNullOrWhiteSpace(key);
    }

    private void RenderTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = (RenderTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
        ExteriorOptions.Visibility = selected == "Interior"
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private async void RenderButton_Click(object sender, RoutedEventArgs e)
    {
        if (_uiApp is null)
        {
            ShowStatus("Revit connection not available. Please restart Revit.", isError: true);
            return;
        }

        var apiKey = CredentialStore.LoadApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ShowStatus("No API key configured. Open Settings to add one.", isError: true);
            return;
        }

        var uiDoc = _uiApp.ActiveUIDocument;
        if (uiDoc is null)
        {
            ShowStatus("No document is open.", isError: true);
            return;
        }

        var view = ViewExporter.GetActive3DView(uiDoc);
        if (view is null)
        {
            ShowStatus("Please activate a 3D view before generating a render.", isError: true);
            return;
        }

        SetBusy(true);

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            ShowStatus("Exporting view...");
            byte[] imageBytes = null!;

            // Export must run on Revit's API thread — we're already there since this is a UI event
            // triggered from the dockable pane, which runs in Revit's context.
            imageBytes = ViewExporter.ExportToPng(uiDoc.Document, view);

            ShowStatus("Sending to ArchRender...");

            var options = BuildOptions();
            var client = new ApiClient(apiKey);
            var result = await client.GenerateRenderAsync(imageBytes, options, _cts.Token);

            _lastResultUrl = result.ImageUrl;
            await ShowResultAsync(result.ImageUrl);

            if (result.CreditsRemaining >= 0)
            {
                CreditsText.Text = $"{result.CreditsRemaining} credits remaining";
                CreditsText.Visibility = Visibility.Visible;
            }

            StatusText.Visibility = Visibility.Collapsed;
        }
        catch (OperationCanceledException)
        {
            ShowStatus("Cancelled.", isError: false);
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            ShowStatus("Invalid API key. Please check your settings.", isError: true);
        }
        catch (ApiException ex) when (ex.StatusCode == 402)
        {
            ShowStatus("Not enough credits. Visit archrender.com to top up.", isError: true);
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ShowResultAsync(string imageUrl)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(imageUrl);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        ResultImage.Source = bitmap;
        ResultBorder.Visibility = Visibility.Visible;
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastResultUrl is null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Save ArchRender result",
            Filter = "PNG Image|*.png",
            FileName = $"archrender_{DateTime.Now:yyyyMMdd_HHmmss}.png",
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            using var http = new System.Net.Http.HttpClient();
            var bytes = await http.GetByteArrayAsync(_lastResultUrl);
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            ShowStatus($"Saved to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            ShowStatus($"Save failed: {ex.Message}", isError: true);
        }
    }

    private void ResultImage_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Open full-size in browser on double-click
        if (e.ClickCount == 2 && _lastResultUrl is not null)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_lastResultUrl)
            {
                UseShellExecute = true
            });
    }

    private void MaterialDetailsBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        MaterialDetailsPlaceholder.Visibility = string.IsNullOrEmpty(MaterialDetailsBox.Text)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private RenderOptions BuildOptions() => new()
    {
        RenderType = ((RenderTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Exterior").ToLower(),
        Season = (SeasonCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Summer",
        TimeOfDay = (TimeOfDayCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Noon",
        Environment = (EnvironmentCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Suburban",
        AspectRatio = (AspectRatioCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "4:3",
        MaterialDetails = MaterialDetailsBox.Text.Trim(),
    };

    private void SetBusy(bool busy)
    {
        RenderButton.IsEnabled = !busy;
        ProgressBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowStatus(string message, bool isError = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xf8, 0x71, 0x71))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x9c, 0xa3, 0xaf));
        StatusText.Visibility = Visibility.Visible;
    }
}
