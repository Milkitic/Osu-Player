using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Milki.OsuPlayer.Common;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Shared;
using Milki.OsuPlayer.Wpf;
using NLog.Config;

namespace Milki.OsuPlayer;

public static class EntryStartup
{
    public static async Task StartupAsync()
    {
        ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("InvariantCulture", typeof(InvariantCultureLayoutRendererWrapper));
        if (!LoadConfig())
        {
            Environment.Exit(0);
            return;
        }

#if DEBUG
        //ConsoleManager.Show();
#endif

        await InitLocalDb();

        StyleUtilities.SetAlignment();

        //https://ffmpeg.zeranoe.com/builds/win32/shared/ffmpeg-4.2.1-win32-shared.zip
        Unosquare.FFME.Library.FFmpegDirectory = Path.Combine(Domain.PluginPath, "ffmpeg");
    }

    private static bool LoadConfig()
    {
        try
        {
            _ = AppSettings.Default;
            return true;
        }
        catch (Exception ex)
        {
            var result = MsgDialog.WarnOkCancel("Error occurs while loading configuration. " +
                                                "Click 'OK' to override current configuration.",
                instruction: "Invalid configuration",
                title: "Osu Player",
                detail: "Exception message: " + ex.Message);
            if (result)
            {
                File.Delete("./AppSettings.yaml");
                return true;
            }

            return false;
        }
    }

    private static async Task InitLocalDb()
    {
        await using var dbContext = new ApplicationDbContext();
        await dbContext.Database.MigrateAsync();
    }
}