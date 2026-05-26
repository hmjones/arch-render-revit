using System;
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
        try
        {
            var hwnd = commandData.Application.MainWindowHandle;
            var window = new SettingsWindow();

            // Host inside Revit's window so it stays on top correctly
            if (hwnd != IntPtr.Zero)
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(window);
                helper.Owner = hwnd;
            }

            window.ShowDialog();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
