using System.IO;
using Autodesk.Revit.DB;

namespace ArchRender.Revit.Services;

public static class ViewExporter
{
    /// <summary>
    /// Exports the given 3D view to a PNG and returns the raw bytes.
    /// Revit appends the view name to the base file path, so we glob for the result.
    /// </summary>
    public static byte[] ExportToPng(Document doc, View3D view, int pixelSize = 2048)
    {
        var tempDir = Path.GetTempPath();
        var baseName = $"archrender_export_{Guid.NewGuid():N}";
        var basePath = Path.Combine(tempDir, baseName);

        var options = new ImageExportOptions
        {
            ExportRange = ExportRange.SetOfViews,
            FilePath = basePath,
            FitDirection = FitDirectionType.Horizontal,
            HLRandWFViewsFileType = ImageFileType.PNG,
            ImageResolution = ImageResolution.DPI_150,
            PixelSize = pixelSize,
            ZoomType = ZoomFitType.Zoom,
            Zoom = 100,
            ShadowViewsFileType = ImageFileType.PNG,
        };

        options.SetViewsAndSheets(new List<ElementId> { view.Id });
        doc.ExportImage(options);

        // Revit appends " - {ViewName}" to the base path
        var exported = Directory.GetFiles(tempDir, baseName + "*.png");

        if (exported.Length == 0)
            throw new InvalidOperationException("Revit did not produce an export file. Make sure the active view is a 3D view.");

        var bytes = File.ReadAllBytes(exported[0]);

        foreach (var f in exported)
            File.Delete(f);

        return bytes;
    }

    /// <summary>
    /// Returns the active 3D view, or null if no 3D view is active.
    /// </summary>
    public static View3D? GetActive3DView(Autodesk.Revit.UI.UIDocument uiDoc)
    {
        return uiDoc.ActiveView as View3D;
    }
}
