using Milky.OsuPlayer.Shared.Dependency;

namespace Milky.OsuPlayer.Services
{
    public static class AppServices
    {
        private static readonly IAppNotificationService FallbackNotifications = new AppNotificationService();
        private static readonly IPlayerDataService FallbackPlayerData =
            new NotifyingPlayerDataService(new PlayerDataService(), FallbackNotifications);

        public static IAppNotificationService Notifications => Service.Get<IAppNotificationService>() ?? FallbackNotifications;

        public static IPlayerDataService PlayerData => Service.Get<IPlayerDataService>() ?? FallbackPlayerData;

        public static void RegisterDefaults()
        {
            var notifications = new AppNotificationService();
            var playerData = new NotifyingPlayerDataService(new PlayerDataService(), notifications);

            Service.AddOrUpdateInstance<IAppNotificationService>(notifications, (_, _) => notifications);
            Service.AddOrUpdateInstance<IPlayerDataService>(playerData, (_, _) => playerData);
        }
    }
}