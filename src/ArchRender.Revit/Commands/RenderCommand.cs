using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ArchRender.Revit.Commands;

[Transaction(TransactionMode.ReadOnly)]
public class RenderCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiApp = commandData.Application;
        var pane = uiApp.GetDockablePane(App.RenderPaneId);
        pane.Show();
        return Result.Succeeded;
    }
}
