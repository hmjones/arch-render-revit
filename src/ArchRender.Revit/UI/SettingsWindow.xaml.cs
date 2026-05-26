using System.Windows;
using ArchRender.Revit.Services;

namespace ArchRender.Revit.UI;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        var existing = CredentialStore.LoadApiKey();
        if (!string.IsNullOrWhiteSpace(existing))
            ApiKeyBox.Password = existing;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var key = ApiKeyBox.Password.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            ShowStatus("Please enter an API key.", error: true);
            return;
        }

        CredentialStore.SaveApiKey(key);
        ShowStatus("API key saved.", error: false);
        NotifyRenderPane();
    }

    private void ClearKey_Click(object sender, RoutedEventArgs e)
    {
        CredentialStore.Clear();
        ApiKeyBox.Password = "";
        ShowStatus("API key cleared.", error: false);
        NotifyRenderPane();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private static void NotifyRenderPane()
    {
        // Refresh the render pane's button/banner state after key changes
        if (System.Windows.Application.Current?.MainWindow is { } main)
        {
            var pane = main.FindName("RenderPaneContent") as RenderPane;
            pane?.RefreshApiKeyState();
        }
    }

    private void ShowStatus(string message, bool error)
    {
        StatusText.Text = message;
        StatusText.Foreground = error
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xf8, 0x71, 0x71))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4a, 0xde, 0x80));
        StatusText.Visibility = Visibility.Visible;
    }
}
