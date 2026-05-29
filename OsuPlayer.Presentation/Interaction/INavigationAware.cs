namespace Milky.OsuPlayer.Presentation.Interaction
{
    /// <summary>
    /// 允许 ViewModel 在导航发生时接收上下文参数
    /// </summary>
    public interface INavigationAware
    {
        void OnNavigatedTo(object parameter);
    }
}
