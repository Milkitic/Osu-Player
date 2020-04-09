using Microsoft.Win32;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Data;
using OSharp.Beatmap;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Milky.OsuPlayer.Common
{
    public static class CommonUtils
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static AppDbOperator _appDbOperator = new AppDbOperator();
        private static readonly SemaphoreSlim Lock = new SemaphoreSlim(5);
        ///// <summary>
        ///// Copy resource to folder
        ///// </summary>
        ///// <param name="filename">File name in resource.</param>
        ///// <param name="path">Path to save.</param>
        //public static void ExportResource(string filename, string path)
        //{
        //    System.Resources.ResourceManager rm = Properties.Resources.ResourceManager;
        //    byte[] obj = (byte[])rm.GetObject(filename, null);
        //    if (obj == null)
        //        return;
        //    using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
        //    {
        //        fs.Write(obj, 0, obj.Length);
        //        fs.Close();
        //    }
        //}

        public static bool? BrowseDb(out string path)
        {
            var fbd = new OpenFileDialog
            {
                Title = @"请选择osu所在目录内的""osu!.db""",
                Filter = @"Beatmap Database|osu!.db"
            };
            var result = fbd.ShowDialog();
            path = fbd.FileName;
            return result;
        }

        public static async Task<string> GetThumbByBeatmapDbId(BeatmapDataModel dataModel)
        {
            return await Task.Run(async () =>
            {
                await Lock.WaitAsync();
                try
                {
                    if (_appDbOperator.GetMapThumb(dataModel.BeatmapDbId, out var path) && path != null)
                    {
                        if (File.Exists(path)) return path;
                    }

                    var folder = dataModel.GetFolder(out var isFromDb, out var freePath);
                    var osuFilePath = isFromDb ? Path.Combine(folder, dataModel.BeatmapFileName) : freePath;

                    if (!File.Exists(osuFilePath))
                    {
                        return null;
                    }

                    var osuFile = await OsuFile.ReadFromFileAsync(osuFilePath, options =>
                        {
                            options.IncludeSection("Events");
                            options.IgnoreSample();
                            options.IgnoreStoryboard();
                        })
                        .ConfigureAwait(false);
                    if (!osuFile.ReadSuccess) return null;

                    var guidStr = Guid.NewGuid().ToString();

                    var sourceBgFile = osuFile.Events?.BackgroundInfo?.Filename;
                    if (string.IsNullOrWhiteSpace(sourceBgFile))
                    {
                        _appDbOperator.SetMapThumb(dataModel.BeatmapDbId, null);
                        return null;
                    }

                    var sourceBgPath = Path.Combine(folder, sourceBgFile);

                    if (!File.Exists(sourceBgPath))
                    {
                        //_appDbOperator.SetMapThumb(dataModel.BeatmapDbId, null);
                        return null;
                    }

                    ResizeImageAndSave(sourceBgPath, guidStr, height: 200);
                    _appDbOperator.SetMapThumb(dataModel.BeatmapDbId, guidStr);
                    return guidStr;
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while creating beatmap thumb cache: {0}", dataModel.GetIdentity());
                    return default;
                }
                finally
                {
                    Lock.Release();
                }
            });
        }

        public static Duration GetDuration(TimeSpan ts)
        {
            if (AppSettings.Default.Interface.MinimalMode)
                return new Duration(TimeSpan.Zero);
            return new Duration(ts);
        }

        private static void ResizeImageAndSave(string sourcePath, string targetName, int width = 0, int height = 0)
        {
            byte[] imageBytes = LoadImageData(sourcePath);
            BitmapSource bitmapSource = CreateImage(imageBytes, width, height);
            imageBytes = GetEncodedImageData(bitmapSource, ".jpg");
            SaveImageData(imageBytes, Path.Combine(Domain.ThumbCachePath, $"{targetName}.jpg"));
        }

        private static byte[] LoadImageData(string filePath)
        {
            byte[] imageBytes;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                imageBytes = br.ReadBytes((int)fs.Length);
            }

            return imageBytes;
        }

        private static void SaveImageData(byte[] imageData, string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(imageData);
            }
        }

        private static BitmapSource CreateImage(byte[] imageData, int decodePixelWidth, int decodePixelHeight)
        {
            if (imageData == null) return null;
            BitmapImage result = new BitmapImage();
            result.BeginInit();
            if (decodePixelWidth > 0)
            {
                result.DecodePixelWidth = decodePixelWidth;
            }

            if (decodePixelHeight > 0)
            {
                result.DecodePixelHeight = decodePixelHeight;
            }

            result.StreamSource = new MemoryStream(imageData);
            result.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            result.CacheOption = BitmapCacheOption.Default;
            result.EndInit();
            return result;
        }

        private static byte[] GetEncodedImageData(BitmapSource source, string preferredFormat)
        {
            byte[] result = null;
            BitmapEncoder encoder;
            switch (preferredFormat.ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;
                case ".tif":
                case ".tiff":
                    encoder = new TiffBitmapEncoder();
                    break;
                case ".gif":
                    encoder = new GifBitmapEncoder();
                    break;
                case ".wmp":
                    encoder = new WmpBitmapEncoder();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            encoder.Frames.Add(BitmapFrame.Create(source));

            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                result = new byte[stream.Length];
                using (var br = new BinaryReader(stream))
                {
                    br.Read(result, 0, (int)stream.Length);
                }
            }

            return result;
        }
    }
}
