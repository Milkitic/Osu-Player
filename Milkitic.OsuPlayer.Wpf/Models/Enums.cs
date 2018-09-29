namespace Milkitic.OsuPlayer.Wpf.Models
{
    public enum SortMode
    {
        Artist, Title
    }

    public enum PlayerStatus
    {
        NotInitialized, Ready, Playing, Paused, Stopped,Finished
    }

    public enum PlayerMode
    {
        Normal, Random, Loop, LoopRandom, Single, SingleLoop,
    }

    public enum PlayListMode
    {
        RecentList, Collection
    }
}
