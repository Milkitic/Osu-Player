using SharpDX;
using SharpDX.DXGI;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using D2D = SharpDX.Direct2D1;
using DXIO = SharpDX.IO;
using GdiP = System.Drawing;
using GdiPimaging = System.Drawing.Imaging;
using WIC = SharpDX.WIC;

namespace OsbPlayerTest.Util
{
    public static class Loader
    {
        private static readonly ConcurrentDictionary<string, D2D.Bitmap> Cached =
            new ConcurrentDictionary<string, D2D.Bitmap>();
        //private readonly object _lockObj = new object();

        public static D2D.Bitmap LoadBitmap(D2D.RenderTarget renderTarget, string imagePath)
        {
            FileInfo fi = new FileInfo(imagePath);
            if (!fi.Exists)
            {
                var ext = fi.Extension;
                string newExt = "";
                if (ext == ".jpg")
                    newExt = ".png";
                else if (ext == ".png")
                    newExt = ".jpg";
                string newName = fi.FullName.Remove(fi.FullName.Length - 4, 4);
                imagePath = newName + newExt;
            }

            D2D.Bitmap bmp;
            if (Cached.ContainsKey(imagePath))
                bmp = Cached[imagePath];
            else
            {
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
            }

            if (!Cached.ContainsKey(imagePath))
            {
                Cached.TryAdd(imagePath, bmp);
                LogUtil.LogInfo("Created cache.");
            }

            return bmp;
        }

        public static D2D.Bitmap LoadBitmap(D2D.RenderTarget renderTarget, GdiP.Bitmap bitmap)
        {
            //lock (_lockObj)
            {
                var sourceArea = new GdiP.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bitmapProperties =
                    new D2D.BitmapProperties(new D2D.PixelFormat(Format.R8G8B8A8_UNorm, D2D.AlphaMode.Premultiplied));
                Size2 size = new Size2(bitmap.Width, bitmap.Height);

                // Transform pixels from BGRA to RGBA
                int stride = bitmap.Width * sizeof(int);
                using (var tempStream = new DataStream(bitmap.Height * stride, true, true))
                {
                    // Lock System.Drawing.Bitmap
                    var bitmapData = bitmap.LockBits(sourceArea, GdiPimaging.ImageLockMode.ReadOnly,
                        GdiPimaging.PixelFormat.Format32bppPArgb);

                    // Convert all pixels 
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        int offset = bitmapData.Stride * y;
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            // Not optimized 
                            byte b = Marshal.ReadByte(bitmapData.Scan0, offset++);
                            byte g = Marshal.ReadByte(bitmapData.Scan0, offset++);
                            byte r = Marshal.ReadByte(bitmapData.Scan0, offset++);
                            byte a = Marshal.ReadByte(bitmapData.Scan0, offset++);
                            int rgba = r | (g << 8) | (b << 16) | (a << 24);
                            tempStream.Write(rgba);
                        }
                    }

                    bitmap.UnlockBits(bitmapData);
                    tempStream.Position = 0;

                    return new D2D.Bitmap(renderTarget, size, tempStream, stride, bitmapProperties);
                }
            }
        }

        [Obsolete("Low-Performance")]
        public static D2D.Bitmap LoadFromFile(D2D.RenderTarget renderTarget, string file)
        {
            // Loads from file using System.Drawing.Image
            using (var bitmap = (GdiP.Bitmap)GdiP.Image.FromFile(file))
            {
                return LoadBitmap(renderTarget, bitmap);
            }
        }

    }
}
