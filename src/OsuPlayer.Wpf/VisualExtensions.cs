using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace Milki.OsuPlayer.Wpf;

public static class VisualExtensions
{
    public static Bitmap GenerateScreenShot(this FrameworkElement fe)
    {
        var dpi = GetDpi(fe);
        var bmp = new RenderTargetBitmap((int)fe.ActualWidth, (int)fe.ActualHeight,
            dpi.X, dpi.Y, PixelFormats.Pbgra32);

        bmp.Render(fe);

        var stream = new MemoryStream();
        BitmapEncoder encoder = new BmpBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        encoder.Save(stream);
        var bitmap = new Bitmap(stream);
        return bitmap;
    }

    private static Point GetDpi(Visual visual)
    {
        var source = PresentationSource.FromVisual(visual);

        double dpiX = 96, dpiY = 96;
        if (source is { CompositionTarget: { } })
        {
            dpiX *= source.CompositionTarget.TransformToDevice.M11;
            dpiY *= source.CompositionTarget.TransformToDevice.M22;
        }

        return new Point(dpiX, dpiY);
    }
}