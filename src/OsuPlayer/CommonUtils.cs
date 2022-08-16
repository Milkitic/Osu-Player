using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Coosu.Beatmap;
using Coosu.Database.DataTypes;
using Microsoft.Win32;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;

namespace Milki.OsuPlayer;

public static class CommonUtils
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
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

    public static async Task<string> GetThumbByBeatmapDbId(Beatmap beatmap)
    {
        return await Task.Run(async () =>
        {
            await Lock.WaitAsync();
            try
            {
                await using var dbContext = new ApplicationDbContext();
                var thumb = await dbContext.GetThumb(beatmap);
                if (thumb != null)
                {
                    if (File.Exists(thumb.ThumbPath)) return thumb.ThumbPath;
                }

                var folder = beatmap.GetFolder(out var isFromDb, out var freePath);
                var osuFilePath = isFromDb ? Path.Combine(folder, beatmap.BeatmapFileName) : freePath;

                if (!File.Exists(osuFilePath))
                {
                    return null;
                }

                LocalOsuFile osuFile;
                try
                {
                    osuFile = await OsuFile.ReadFromFileAsync(osuFilePath, options =>
                        {
                            options.IncludeSection("Events");
                            options.IgnoreSample();
                            options.IgnoreStoryboard();
                        })
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    return null;
                }

                var guidStr = Guid.NewGuid().ToString();

                var sourceBgFile = osuFile.Events?.BackgroundInfo?.Filename;
                if (string.IsNullOrWhiteSpace(sourceBgFile))
                {
                    await dbContext.AddOrUpdateThumbPath(beatmap, null);
                    return null;
                }

                var sourceBgPath = Path.Combine(folder, sourceBgFile);

                if (!File.Exists(sourceBgPath))
                {
                    //_appDbOperator.SetMapThumb(dataModel.BeatmapDbId, null);
                    return null;
                }

                ResizeImageAndSave(sourceBgPath, guidStr, height: 200);
                await dbContext.AddOrUpdateThumbPath(beatmap, guidStr);
                return guidStr;
            }
            catch (Exception ex)
            {
                Logger.Error("Error while creating beatmap thumb cache: {0}", beatmap.ToString());
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
        if (AppSettings.Default == null) return TimeSpan.Zero;
        if (AppSettings.Default.Interface.MinimalMode)
            return new Duration(TimeSpan.Zero);
        return new Duration(ts);
    }

    private static void ResizeImageAndSave(string sourcePath, string targetName, int width = 0, int height = 0)
    {
        byte[] imageBytes = LoadImageData(sourcePath);
        BitmapSource bitmapSource = CreateImage(imageBytes, width, height);
        imageBytes = GetEncodedImageData(bitmapSource, ".jpg");
        var filePath = Path.Combine(AppSettings.Directories.ThumbCacheDir, $"{targetName}.jpg");
        SaveImageData(imageBytes, filePath);
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