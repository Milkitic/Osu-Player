using System;
using KeyAsio.Core.Audio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Instances;
using OsuPlayer.Core.Scanning;
using OsuPlayer.Core.Services;
using OsuPlayer.Data;
using OsuPlayer.Instances;
using OsuPlayer.Lang;
using OsuPlayer.Localization;
using OsuPlayer.Media.Audio;
using OsuPlayer.Playback;
using OsuPlayer.Playback.Playlist;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Services;
using OsuPlayer.Shared;
using OsuPlayer.ViewModels;
using OsuPlayer.Views.Pages;
using OsuPlayer.Views.Pages.Settings;
using OsuPlayer.Windows;

namespace OsuPlayer;

public static class EntrySetup
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        LocalizationService.Instance.ConfigureStringResolver(static key => SR.ResourceManager.GetString(key) ?? key);
        LocalizationService.Instance.ConfigureCultureApplier(static culture => SR.Culture = culture);

        services.AddLogging(static loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            loggingBuilder.AddNLog();
        });

        services.AddOsuPlayerAudioModule();
        services.Replace(ServiceDescriptor.Singleton<IAudioDeviceManager, OsuPlayerAudioDeviceManager>());
        services.AddDatabaseServices();
        services.AddApplicationServices();
        services.AddViewModels();
        return services;
    }

    private static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        services.AddTransient<OsuPlayerDbContext>(static provider =>
        {
            var options = new DbContextOptionsBuilder<OsuPlayerDbContext>()
                .UseSqlite(OsuPlayerDbContext.DefaultConnectionString)
                .Options;
            return new OsuPlayerDbContext(options, provider.GetRequiredService<ILogger<OsuPlayerDbContext>>());
        });
        services.AddSingleton<Func<OsuPlayerDbContext>>(static provider =>
            () => provider.GetRequiredService<OsuPlayerDbContext>());
        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IBeatmapThumbnailService, BeatmapThumbnailService>();
        services.AddSingleton<IMapModelConverter, MapModelConverter>();
        services.AddSingleton<BeatmapLoader>();

        services.AddSingleton<IAppNotificationService>(AppNotificationService.Instance);
        services.AddSingleton<ILanguagePreferenceStore, AppSettingsLanguagePreferenceStore>();
        services.AddSingleton<LanguageManager>();
        services.AddTransient<INavigationService, FrameNavigationService>();
        services.AddSingleton<IUiThreadDispatcher>(_ => Execute.UiThreadDispatcher);
        services.AddSingleton<IUserPreferences>(_ =>
            AppSettings.Default ?? throw new InvalidOperationException("AppSettings is not loaded."));
        services.AddSingleton<IAppPaths>(_ => AppPaths.Current);

        services.AddSingleton<PlayerDataService>();
        services.AddSingleton<IPlayerDataStore>(static provider => provider.GetRequiredService<PlayerDataService>());
        services.AddSingleton<NotifyingPlayerDataService>();
        services.AddSingleton<IPlayerDataService>(static provider =>
            provider.GetRequiredService<NotifyingPlayerDataService>());

        services.AddSingleton<PlayerEventBus>();
        services.AddSingleton<PlayList>();
        services.AddSingleton<PlayerSessionService>();
        services.AddSingleton<ObservablePlayController>();
        services.AddSingleton<IPlaybackController>(static provider =>
            provider.GetRequiredService<ObservablePlayController>());

        services.AddSingleton<OsuDbInst>();
        services.AddSingleton<LyricsInst>();
        services.AddSingleton<UpdateInst>();
        services.AddSingleton<OsuFileScanner>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IBeatmapDifficultyPicker, BeatmapDifficultyPicker>();
        services.AddSingleton<IBeatmapActionService, BeatmapActionService>();

        return services;
    }

    private static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddSingleton(_ => SharedVm.Default);
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<OsuPlayer.Views.UserControls.PlayControllerVm>();
        services.AddSingleton<OsuPlayer.Views.UserControls.PlayListControlVm>();

        services.AddTransient<CollectionPageViewModel>();
        services.AddTransient<SearchPageViewModel>();
        services.AddTransient<RecentPlayPageViewModel>();
        services.AddTransient<ExportPageViewModel>();

        services.AddTransient<OsuPlayer.ViewModels.Pages.Settings.InterfacePageViewModel>();
        services.AddTransient<OsuPlayer.ViewModels.Pages.Settings.AboutPageViewModel>();
        services.AddTransient<OsuPlayer.ViewModels.Pages.Settings.GeneralPageViewModel>();
        services.AddTransient<OsuPlayer.ViewModels.Pages.Settings.PlayPageViewModel>();
        services.AddTransient<OsuPlayer.ViewModels.Pages.Settings.LyricPageViewModel>();
        services.AddTransient<OsuPlayer.ViewModels.Pages.Settings.ExportPageViewModel>();
        services.AddTransient<OsuPlayer.ViewModels.Pages.Settings.HotKeyPageViewModel>();

        // 页面 View 注册
        services.AddSingleton<MainWindow>();
        services.AddTransient<CollectionPage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<RecentPlayPage>();
        services.AddTransient<FindPage>();
        services.AddTransient<OsuPlayer.Views.Pages.ExportPage>();

        services.AddTransient<AboutPage>();
        services.AddTransient<GeneralPage>();
        services.AddTransient<InterfacePage>();
        services.AddTransient<PlayPage>();
        services.AddTransient<LyricPage>();
        services.AddTransient<OsuPlayer.Views.Pages.Settings.ExportPage>();
        services.AddTransient<HotKeyPage>();
        services.AddTransient<ConfigWindow>();

        return services;
    }
}
