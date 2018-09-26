namespace Milkitic.OsuPlayer.Wpf.Models
{
    public enum SortMode
    {
        Artist, Title
    }

    public enum PlayerStatus
    {
        NotInitialized, Ready, Playing, Paused, Stopped
    }

    internal enum PlayerMode
    {
        Normal, Random, Loop, LoopRandom
    }

    public enum PlayListMode
    {
        RecentList, Collection
    }
}
