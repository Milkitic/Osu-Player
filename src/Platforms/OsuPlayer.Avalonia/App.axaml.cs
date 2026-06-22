using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Core;
using OsuPlayer.Lang;
using OsuPlayer.Localization;
using OsuPlayer.Media.Audio;
using OsuPlayer.Playback;
using OsuPlayer.Video;
using OsuPlayer.ViewModels;
using OsuPlayer.Windows;

namespace OsuPlayer;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private static MainWindow? s_mainWindow;
    private static TrayIcon? s_trayIcon;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Services = new ServiceCollection()
                .ConfigureServices()
                .BuildServiceProvider();

            try
            {
                await EntryStartup.StartupAsync(Services);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Startup failed: {ex}");
                Environment.Exit(1);
                return;
            }

            Services.GetRequiredService<LanguageManager>();

            // 初始化 FFmpeg(视频播放)
            VideoPlayerInitializer.Initialize();

            var controller = Services.GetRequiredService<ObservablePlayController>();
            controller.PlayStatusChanged += status =>
            {
                SharedVm.Default.IsPlaying = status == PlayStatus.Playing;
            };

            s_mainWindow = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = s_mainWindow;

            // 程序化创建并附加托盘图标
            SetupTrayIcon();

            desktop.ShutdownRequested += (_, _) =>
            {
                OsuPlayer.Core.Configuration.AppSettings.Default?.Dispose();
                NLog.LogManager.Shutdown();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void SetupTrayIcon()
    {
        // 1) 加载图标
        WindowIcon? icon = null;
        try
        {
            var iconUri = new Uri("avares://OsuPlayer/osuPlayer.ico");
            using var stream = AssetLoader.Open(iconUri);
            icon = new WindowIcon(stream);
        }
        catch
        {
            // 图标加载失败,使用 null
        }

        // 2) 构造菜单
        var menu = new NativeMenu();
        var showItem = new NativeMenuItem("Show / Hide Osu Player");
        showItem.Click += (_, _) => ToggleMainWindow();
        menu.Items.Add(showItem);
        var settingsItem = new NativeMenuItem(I18NUtil.GetString(SRKeys.Ui_Sets));
        settingsItem.Click += (_, _) => OpenSettingsWindow();
        menu.Items.Add(settingsItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        var exitItem = new NativeMenuItem(I18NUtil.GetString(SRKeys.Ui_Exit));
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        // 3) 创建 TrayIcon 并附加到 Application
        s_trayIcon = new TrayIcon
        {
            ToolTipText = "Osu Player",
            Menu = menu,
            Icon = icon,
            IsVisible = true
        };

        if (Application.Current is App app)
        {
            TrayIcon.SetIcons(app, new TrayIcons { s_trayIcon });
        }
    }

    private static void ToggleMainWindow()
    {
        if (s_mainWindow == null) return;
        if (s_mainWindow.IsVisible)
        {
            s_mainWindow.Hide();
        }
        else
        {
            s_mainWindow.Show();
            s_mainWindow.WindowState = WindowState.Normal;
            s_mainWindow.Activate();
        }
    }

    private static void OpenSettingsWindow()
    {
        if (s_mainWindow == null)
        {
            return;
        }

        if (!s_mainWindow.IsVisible)
        {
            s_mainWindow.Show();
            s_mainWindow.WindowState = WindowState.Normal;
        }

        s_mainWindow.Activate();
        s_mainWindow.OpenSettingsWindow();
    }

    private static void ExitApp()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }
}
