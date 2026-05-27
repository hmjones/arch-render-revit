using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ArchRender.Revit.UI;

public partial class RenderResultWindow : Window
{
    private string? _imageUrl;

    public RenderResultWindow()
    {
        InitializeComponent();
    }

    public void SetLoadingText(string text)
    {
        Dispatcher.Invoke(() =>
        {
            LoadingPanel.Visibility = Visibility.Visible;
            LoadingText.Text = text;
            ResultImage.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
        });
    }

    public async Task LoadResultAsync(string imageUrl, int? creditsRemaining)
    {
        _imageUrl = imageUrl;

        try
        {
            using var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(imageUrl);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(bytes);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            await Dispatcher.InvokeAsync(() =>
            {
                ResultImage.Source = bitmap;
                LoadingPanel.Visibility = Visibility.Collapsed;
                ResultImage.Visibility = Visibility.Visible;
                ErrorPanel.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = true;
                OpenInBrowserButton.IsEnabled = true;

                if (creditsRemaining.HasValue)
                    CreditsText.Text = $"{creditsRemaining.Value} credits remaining";
            });
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load image: {ex.Message}");
        }
    }

    public void ShowError(string message)
    {
        Dispatcher.Invoke(() =>
        {
            ErrorText.Text = message;
            ErrorPanel.Visibility = Visibility.Visible;
            LoadingPanel.Visibility = Visibility.Collapsed;
            ResultImage.Visibility = Visibility.Collapsed;
        });
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_imageUrl is null) return;

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
            var bytes = await http.GetByteArrayAsync(_imageUrl);
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed: {ex.Message}", "ArchRender",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenInBrowserButton_Click(object sender, RoutedEventArgs e)
    {
        if (_imageUrl is null) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_imageUrl)
        {
            UseShellExecute = true
        });
    }

    private void ResultImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) OpenInBrowserButton_Click(sender, e);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
