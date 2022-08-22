using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Data;

namespace Milki.OsuPlayer;

public static class ServiceProviders
{
    public static IServiceProvider Default => App.Current.ServiceProvider;
    public static ApplicationDbContext GetApplicationDbContext() => new ApplicationDbContext();
}