using Milky.OsuPlayer.Shared.Dependency;

namespace Milky.OsuPlayer.Services
{
    public static class AppServices
    {
        private static readonly IAppNotificationService FallbackNotifications = new AppNotificationService();
        private static readonly IPlayerDataStore FallbackPlayerDataStore = new PlayerDataService();
        private static readonly IPlayerDataService FallbackPlayerData =
            new NotifyingPlayerDataService(FallbackPlayerDataStore, FallbackNotifications);

        public static IAppNotificationService Notifications => Service.Get<IAppNotificationService>() ?? FallbackNotifications;

        public static IPlayerDataStore PlayerDataStore => Service.Get<IPlayerDataStore>() ?? FallbackPlayerDataStore;

        public static IPlayerDataService PlayerData => Service.Get<IPlayerDataService>() ?? FallbackPlayerData;

        public static void RegisterDefaults()
        {
            var notifications = new AppNotificationService();
            var playerDataStore = new PlayerDataService();
            var playerData = new NotifyingPlayerDataService(playerDataStore, notifications);

            Service.AddOrUpdateInstance<IAppNotificationService>(notifications, (_, _) => notifications);
            Service.AddOrUpdateInstance<IPlayerDataStore>(playerDataStore, (_, _) => playerDataStore);
            Service.AddOrUpdateInstance<IPlayerDataService>(playerData, (_, _) => playerData);
        }
    }
}