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

    public RenderPane()
    {
        InitializeComponent();
        RenderTypeCombo.SelectionChanged += RenderTypeCombo_SelectionChanged;
        IsVisibleChanged += (_, _) => CheckApiKey();
        CheckApiKey();
    }

    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = this;
        data.InitialState = new DockablePaneState
        {
            DockPosition = DockPosition.Right,
            MinimumWidth = 280,
        };
    }

    internal void RefreshApiKeyState() => CheckApiKey();

    private void CheckApiKey()
    {
        var key = CredentialStore.LoadApiKey();
        var hasKey = !string.IsNullOrWhiteSpace(key);
        NoKeyBanner.Visibility = hasKey ? Visibility.Collapsed : Visibility.Visible;
        RenderButton.IsEnabled = hasKey;
    }

    private void RenderTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = (RenderTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (ExteriorOptions != null)
            ExteriorOptions.Visibility = selected == "Interior"
                ? Visibility.Collapsed
                : Visibility.Visible;
    }

    private void RenderButton_Click(object sender, RoutedEventArgs e)
    {
        var apiKey = CredentialStore.LoadApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ShowStatus("No API key configured. Open Settings to add one.", isError: true);
            return;
        }

        if (App.RenderEvent is null)
        {
            ShowStatus("Revit event handler not initialised. Please restart Revit.", isError: true);
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var options = BuildOptions();

        SetBusy(true);
        ShowStatus("Exporting view...");

        // Wire up callbacks — these fire on the Revit API thread, then we dispatch back to UI
        App.RenderHandler.OnExported = async (bytes) =>
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                ShowStatus("Sending to ArchRender...");
                try
                {
                    var client = new ApiClient(apiKey);
                    var result = await client.GenerateRenderAsync(bytes, options, _cts.Token);
                    _lastResultUrl = result.ImageUrl;
                    await ShowResultImageAsync(result.ImageUrl);

                    if (result.CreditsRemaining >= 0)
                    {
                        CreditsText.Text = $"{result.CreditsRemaining} credits remaining";
                        CreditsText.Visibility = Visibility.Visible;
                    }

                    StatusText.Visibility = Visibility.Collapsed;
                }
                catch (OperationCanceledException)
                {
                    ShowStatus("Cancelled.");
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
            });
        };

        App.RenderHandler.OnError = (msg) =>
        {
            Dispatcher.Invoke(() =>
            {
                ShowStatus(msg, isError: true);
                SetBusy(false);
            });
        };

        App.RenderEvent.Raise();
    }

    private async Task ShowResultImageAsync(string imageUrl)
    {
        // Download the bytes first so the BitmapImage is fully loaded (and freezable)
        // before we assign it to the Image control.
        using var http = new HttpClient();
        var bytes = await http.GetByteArrayAsync(imageUrl);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new MemoryStream(bytes);
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
            using var http = new HttpClient();
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
        if (e.ClickCount == 2 && _lastResultUrl is not null)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_lastResultUrl)
            {
                UseShellExecute = true
            });
    }

    private void MaterialDetailsBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (MaterialDetailsPlaceholder != null)
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
        RenderButton.IsEnabled = !busy && !string.IsNullOrWhiteSpace(CredentialStore.LoadApiKey());
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
