using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using ArchRender.Revit.Models;
using ArchRender.Revit.Services;

namespace ArchRender.Revit.UI;

public partial class RenderPane : Page, IDockablePaneProvider
{
    private CancellationTokenSource? _cts;
    private RenderResultWindow? _currentResultWindow;

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
        var targetRatio = ImageCropper.ParseAspectRatio(options.AspectRatio);

        SetBusy(true);

        // Open the result window with the spinner
        _currentResultWindow = new RenderResultWindow();
        var helper = new System.Windows.Interop.WindowInteropHelper(_currentResultWindow);
        helper.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        _currentResultWindow.SetLoadingText("Exporting view...");
        _currentResultWindow.Show();

        var resultWindow = _currentResultWindow;

        App.RenderHandler.OnExported = async (bytes) =>
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    // Crop the source to the target aspect ratio so the AI doesn't warp the building
                    byte[] croppedBytes;
                    try
                    {
                        croppedBytes = ImageCropper.CropToAspectRatio(bytes, targetRatio);
                    }
                    catch (Exception ex)
                    {
                        resultWindow.ShowError($"Failed to crop view: {ex.Message}");
                        SetBusy(false);
                        return;
                    }

                    // Show the cropped source as a dimmed backdrop while the AI runs
                    resultWindow.SetSourceBackdrop(croppedBytes);
                    resultWindow.SetLoadingText("Sending to ArchRender...");

                    var client = new ApiClient(apiKey);
                    var result = await client.GenerateRenderAsync(croppedBytes, options, _cts.Token);

                    resultWindow.SetLoadingText("Loading result...");
                    await resultWindow.LoadResultAsync(result.ImageUrl, result.CreditsRemaining);

                    StatusText.Visibility = Visibility.Collapsed;
                }
                catch (OperationCanceledException)
                {
                    resultWindow.ShowError("Cancelled.");
                }
                catch (ApiException ex) when (ex.StatusCode == 401)
                {
                    resultWindow.ShowError("Invalid API key. Please check your settings.");
                }
                catch (ApiException ex) when (ex.StatusCode == 402)
                {
                    resultWindow.ShowError("Not enough credits. Visit archrender.com to top up.");
                }
                catch (Exception ex)
                {
                    resultWindow.ShowError($"Error: {ex.Message}");
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
                resultWindow.ShowError(msg);
                SetBusy(false);
            });
        };

        App.RenderEvent.Raise();
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
        UseUltraModel = ((QualityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Standard") == "Ultra",
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
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xdc, 0x26, 0x26))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x6b, 0x72, 0x80));
        StatusText.Visibility = Visibility.Visible;
    }
}
