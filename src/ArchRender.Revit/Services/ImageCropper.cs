using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ArchRender.Revit.Services;

public static class ImageCropper
{
    /// <summary>
    /// Centred crop of a PNG to the given aspect ratio (width/height).
    /// Whichever dimension is "too long" relative to the target is trimmed.
    /// </summary>
    public static byte[] CropToAspectRatio(byte[] pngBytes, double targetRatio)
    {
        using var inStream = new MemoryStream(pngBytes);
        var decoder = BitmapDecoder.Create(inStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var source = decoder.Frames[0];

        int srcW = source.PixelWidth;
        int srcH = source.PixelHeight;
        double srcRatio = (double)srcW / srcH;

        if (Math.Abs(srcRatio - targetRatio) < 0.001) return pngBytes;

        int cropW, cropH, x, y;
        if (srcRatio > targetRatio)
        {
            cropH = srcH;
            cropW = (int)(srcH * targetRatio);
            x = (srcW - cropW) / 2;
            y = 0;
        }
        else
        {
            cropW = srcW;
            cropH = (int)(srcW / targetRatio);
            x = 0;
            y = (srcH - cropH) / 2;
        }

        var cropped = new CroppedBitmap(source, new Int32Rect(x, y, cropW, cropH));

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(cropped));
        using var outStream = new MemoryStream();
        encoder.Save(outStream);
        return outStream.ToArray();
    }

    public static double ParseAspectRatio(string ratio) => ratio switch
    {
        "1:1"  => 1.0,
        "4:3"  => 4.0 / 3.0,
        "16:9" => 16.0 / 9.0,
        "3:4"  => 3.0 / 4.0,
        "9:16" => 9.0 / 16.0,
        _      => 4.0 / 3.0,
    };
}
