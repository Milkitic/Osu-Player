using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };

            desktop.ShutdownRequested += (_, _) =>
            {
                OsuPlayer.Core.Configuration.AppSettings.Default?.Dispose();
                NLog.LogManager.Shutdown();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
