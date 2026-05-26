using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ArchRender.Revit.UI;

namespace ArchRender.Revit.Commands;

[Transaction(TransactionMode.ReadOnly)]
public class SettingsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var window = new SettingsWindow
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        window.ShowDialog();
        return Result.Succeeded;
    }
}
