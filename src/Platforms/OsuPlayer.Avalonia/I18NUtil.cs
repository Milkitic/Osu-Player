using System.Collections.Generic;

namespace OsuPlayer.Shared;

/// <summary>
/// Avalonia 版本的 I18NUtil 占位 - 实际加载机制后续通过资源字典替换
/// </summary>
public static class I18NUtil
{
    private static readonly Dictionary<string, string> s_defaultStrings = new()
    {
        { "err-collectionNotInDb", "The collection does not exist in the database." },
        { "text-error", "Error" }
    };

    public static Dictionary<string, string> AvailableLangDic { get; } = new();

    public static void LoadI18N()
    {
        // Avalonia 端: 加载逻辑由 App.axaml 的 ResourceInclude 接管
        AvailableLangDic["English"] = "en-US";
    }

    public static void SwitchToLang(string locale)
    {
        // Avalonia 端: 后续由 ResourceInclude 切换
    }

    public static string GetString(string key)
    {
        return s_defaultStrings.TryGetValue(key, out var s) ? s : "UNBOUND";
    }
}
