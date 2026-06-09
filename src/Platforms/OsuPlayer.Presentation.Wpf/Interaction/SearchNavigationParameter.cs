namespace OsuPlayer.Presentation.Interaction;

/// <summary>
/// 参数化导航到搜索页，避免调用方直接持有页面实例。
/// </summary>
public sealed class SearchNavigationParameter
{
    public SearchNavigationParameter(string keyword)
    {
        Keyword = keyword;
    }

    public string Keyword { get; }
}
