using Autodesk.Revit.UI;
using ArchRender.Revit.UI;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace ArchRender.Revit;

public class App : IExternalApplication
{
    internal static readonly DockablePaneId RenderPaneId =
        new(new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"));

    public Result OnStartup(UIControlledApplication application)
    {
        RegisterDockablePane(application);
        CreateRibbon(application);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

    private static void RegisterDockablePane(UIControlledApplication application)
    {
        var provider = new RenderPane();
        application.RegisterDockablePane(RenderPaneId, "ArchRender", provider);
    }

    private static void CreateRibbon(UIControlledApplication application)
    {
        application.CreateRibbonTab("ArchRender");

        var panel = application.CreateRibbonPanel("ArchRender", "Render");

        var assemblyPath = Assembly.GetExecutingAssembly().Location;

        var renderButton = new PushButtonData(
            "ArchRenderOpen",
            "Render",
            assemblyPath,
            "ArchRender.Revit.Commands.RenderCommand")
        {
            ToolTip = "Open the ArchRender panel to generate AI architectural renders.",
            LargeImage = LoadIcon("render_32.png"),
            Image = LoadIcon("render_16.png"),
        };

        var settingsButton = new PushButtonData(
            "ArchRenderSettings",
            "Settings",
            assemblyPath,
            "ArchRender.Revit.Commands.SettingsCommand")
        {
            ToolTip = "Configure your ArchRender API key.",
            LargeImage = LoadIcon("settings_32.png"),
            Image = LoadIcon("settings_16.png"),
        };

        panel.AddItem(renderButton);
        panel.AddSeparator();
        panel.AddItem(settingsButton);
    }

    private static BitmapImage? LoadIcon(string filename)
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var path = Path.Combine(dir, "Icons", filename);
        if (!File.Exists(path)) return null;
        return new BitmapImage(new Uri(path));
    }
}
