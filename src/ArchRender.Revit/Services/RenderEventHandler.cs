using System.IO;
using Autodesk.Revit.UI;

namespace ArchRender.Revit.Services;

/// <summary>
/// Runs on Revit's API thread in response to ExternalEvent.Raise().
/// Exports the active 3D view and delivers the PNG bytes to the waiting UI callback.
/// </summary>
public class RenderEventHandler : IExternalEventHandler
{
    public Action<byte[]>? OnExported { get; set; }
    public Action<string>? OnError { get; set; }

    public void Execute(UIApplication app)
    {
        try
        {
            var uiDoc = app.ActiveUIDocument;
            if (uiDoc is null) { OnError?.Invoke("No document is open."); return; }

            var view = ViewExporter.GetActive3DView(uiDoc);
            if (view is null) { OnError?.Invoke("Please activate a 3D view before generating a render."); return; }

            var bytes = ViewExporter.ExportToPng(uiDoc.Document, view);
            OnExported?.Invoke(bytes);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex.Message);
        }
    }

    public string GetName() => "ArchRender Export";
}
