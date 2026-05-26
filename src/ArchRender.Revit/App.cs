using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using ArchRender.Revit.Services;
using ArchRender.Revit.UI;

namespace ArchRender.Revit;

public class App : IExternalApplication
{
    internal static readonly DockablePaneId RenderPaneId =
        new(new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"));

    // Accessed by RenderPane to trigger view export on Revit's API thread
    internal static RenderEventHandler RenderHandler { get; } = new();
    internal static ExternalEvent? RenderEvent { get; private set; }

    public Result OnStartup(UIControlledApplication application)
    {
        RenderEvent = ExternalEvent.Create(RenderHandler);
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
            LargeImage = IconGenerator.CreateRenderIcon(32),
            Image = IconGenerator.CreateRenderIcon(16),
        };

        var settingsButton = new PushButtonData(
            "ArchRenderSettings",
            "Settings",
            assemblyPath,
            "ArchRender.Revit.Commands.SettingsCommand")
        {
            ToolTip = "Configure your ArchRender API key.",
            LargeImage = IconGenerator.CreateSettingsIcon(32),
            Image = IconGenerator.CreateSettingsIcon(16),
        };

        panel.AddItem(renderButton);
        panel.AddSeparator();
        panel.AddItem(settingsButton);
    }
}
