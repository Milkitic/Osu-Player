using System.Collections.Concurrent;
using System.IO;
using SharpDX.DXGI;
using D2D = SharpDX.Direct2D1;
using DXIO = SharpDX.IO;
using WIC = SharpDX.WIC;

namespace Milkitic.OsuPlayer.Media.Storyboard.Util
{
    public static class TextureLoader
    {
        private static readonly ConcurrentDictionary<string, D2D.Bitmap> Cached =
            new ConcurrentDictionary<string, D2D.Bitmap>();

        public static D2D.Bitmap LoadBitmap(D2D.RenderTarget renderTarget, string imagePath)
        {
            FileInfo fi = new FileInfo(imagePath);
            if (!fi.Exists)
            {
                var ext = fi.Extension;
                string newExt = "";
                if (ext == ".jpg") newExt = ".png";
                else if (ext == ".png") newExt = ".jpg";
                string newName = fi.FullName.Remove(fi.FullName.Length - 4, 4);
                imagePath = newName + newExt;
            }

            D2D.Bitmap bmp;
            if (Cached.ContainsKey(imagePath))
            {
                bmp = Cached[imagePath];
                if (bmp.IsDisposed)
                    Cached.TryRemove(imagePath, out _);
                else
                    return bmp;
            }

            WIC.ImagingFactory imagingFactory = new WIC.ImagingFactory();
            DXIO.NativeFileStream fileStream = new DXIO.NativeFileStream(imagePath,
                DXIO.NativeFileMode.Open, DXIO.NativeFileAccess.Read);

            WIC.BitmapDecoder bitmapDecoder =
                new WIC.BitmapDecoder(imagingFactory, fileStream, WIC.DecodeOptions.CacheOnDemand);
            WIC.BitmapFrameDecode frame = bitmapDecoder.GetFrame(0);

            WIC.FormatConverter converter = new WIC.FormatConverter(imagingFactory);
            converter.Initialize(frame, WIC.PixelFormat.Format32bppPRGBA);

            var bitmapProperties =
                new D2D.BitmapProperties(new D2D.PixelFormat(Format.R8G8B8A8_UNorm, D2D.AlphaMode.Premultiplied));
            //Size2 size = new Size2(frame.Size.Width, frame.Size.Height);

            bmp = D2D.Bitmap.FromWicBitmap(renderTarget, converter, bitmapProperties);

            if (!Cached.ContainsKey(imagePath))
            {
                Cached.TryAdd(imagePath, bmp);
                //LogUtil.LogInfo("Created cache.");
            }

            return bmp;
        }

        public static void Clear()
        {
            foreach (var bitmap in Cached)
                bitmap.Value?.Dispose();
            Cached.Clear();
        }
    }
}
