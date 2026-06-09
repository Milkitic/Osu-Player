using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Avalonia.ViewModels;
using OsuPlayer.Avalonia.Windows;

namespace OsuPlayer.Avalonia;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

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

            // 加载自定义字体(Source Sans Pro)
            LoadCustomFonts();

            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };

            desktop.ShutdownRequested += (_, _) =>
            {
                OsuPlayer.Core.Configuration.AppSettings.Default?.Dispose();
                NLog.LogManager.Shutdown();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void LoadCustomFonts()
    {
        // Avalonia 12: 通过 Assets index 自动发现 AvaloniaResource 字体
        // 在 MainWindow.axaml 中通过 FontFamily="avares://OsuPlayer.Avalonia/Resources/Fonts#Source Sans Pro" 引用
    }
}
