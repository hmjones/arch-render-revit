using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ArchRender.Revit.UI;

public static class IconGenerator
{
    public static BitmapSource CreateRenderIcon(int size) =>
        CreateIcon(size, Color.FromRgb(37, 99, 235), DrawBuilding);

    public static BitmapSource CreateSettingsIcon(int size) =>
        CreateIcon(size, Color.FromRgb(71, 85, 105), DrawGear);

    private static BitmapSource CreateIcon(int size, Color background, Action<DrawingContext, int> draw)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var brush = new SolidColorBrush(background);
            var rect = new Rect(0, 0, size, size);
            dc.DrawRoundedRectangle(brush, null, rect, size * 0.18, size * 0.18);
            draw(dc, size);
        }

        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        bmp.Freeze();
        return bmp;
    }

    private static void DrawBuilding(DrawingContext dc, int size)
    {
        var white = Brushes.White;
        var s = size / 32.0; // scale factor relative to 32px design

        // Main building body
        var body = new Rect(s * 8, s * 14, s * 16, s * 13);
        dc.DrawRectangle(white, null, body);

        // Roof (triangle)
        var roof = new StreamGeometry();
        using (var ctx = roof.Open())
        {
            ctx.BeginFigure(new Point(s * 4, s * 14), true, true);
            ctx.LineTo(new Point(s * 16, s * 5), true, false);
            ctx.LineTo(new Point(s * 28, s * 14), true, false);
        }
        dc.DrawGeometry(white, null, roof);

        // Door
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(180, 37, 99, 235)), null,
            new Rect(s * 13, s * 20, s * 6, s * 7));

        // Left window
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 37, 99, 235)), null,
            new Rect(s * 9.5, s * 16, s * 4, s * 3.5));

        // Right window
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 37, 99, 235)), null,
            new Rect(s * 18.5, s * 16, s * 4, s * 3.5));
    }

    private static void DrawGear(DrawingContext dc, int size)
    {
        var white = Brushes.White;
        var cx = size / 2.0;
        var cy = size / 2.0;
        var s = size / 32.0;

        // Draw gear using a formatted text character from Segoe UI Symbol
        var typeface = new Typeface(
            new FontFamily("Segoe UI Symbol"),
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        var ft = new FormattedText(
            "⚙", // ⚙ GEAR
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            size * 0.62,
            white,
            96);

        dc.DrawText(ft, new Point(cx - ft.Width / 2, cy - ft.Height / 2));
    }
}
