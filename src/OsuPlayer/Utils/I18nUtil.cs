#nullable enable
using System.Globalization;
using System.IO;
using System.Windows;
using System.Xaml;
using Anotar.NLog;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Shared.Utils;
using XamlReader = System.Windows.Markup.XamlReader;

namespace Milki.OsuPlayer.Utils;

public static class I18NUtil
{
    private static readonly ResourceDictionary? I18NDic =
        Application.Current.Resources.MergedDictionaries.FirstOrDefault(k =>
            k.Source.OriginalString.Contains("i18n.xaml", StringComparison.OrdinalIgnoreCase));

    public static Dictionary<string, string> AvailableLangDic { get; } = new();

    public static ResourceDictionary? CurrentLangDictionary { get; } = I18NDic?.MergedDictionaries.FirstOrDefault();
    public static KeyValuePair<string, string> CurrentLocale { get; private set; }

    public static void SwitchToLang(string locale)
    {
        if (!AvailableLangDic.ContainsValue(locale))
        {
            locale = "en-US";
        }

        ResourceDictionary langRd;
        using (var s = new FileStream(Path.Combine(AppSettings.Directories.LanguageDir, $"{locale}.xaml"),
                   FileMode.Open))
        {
            langRd = (ResourceDictionary)XamlReader.Load(s);
        }

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
        var current = CurrentLangDictionary;
        if (current == null) return;

        foreach (object key in langRd.Keys)
        {
            if (current.Contains(key))
            {
                current[key] = langRd[key];
            }
        }

        CurrentLocale = AvailableLangDic.First(k => k.Value == locale);
    }

    public static string GetString(string key)
    {
        if (CurrentLangDictionary?.Contains(key) == true)
        {
            return (string)CurrentLangDictionary[key];
        }

        return "LOCALE_MISSING";
    }

    public static void InitializeI18NSettings()
    {
        var locale = AppSettings.Default.GeneralSection.Locale;
        var defLocale = CurrentLangDictionary;
        if (defLocale == null) return;

        var defUiStrings = defLocale.Keys.Cast<string>()
            .ToDictionary(defKey => defKey, defKey => defLocale[defKey] as string);

        foreach (var enumerateFile in SharedUtils.EnumerateFiles(AppSettings.Directories.LanguageDir, ".xaml"))
        {
            try
            {
                LoadI18NFile(enumerateFile, defUiStrings);
            }
            catch (Exception ex)
            {
                LogTo.ErrorException($"Error while loading i18n file: {enumerateFile.FullName}", ex);
            }
        }

        if (!AvailableLangDic.ContainsValue("zh-CN"))
        {
            var nativeName = CultureInfo.CreateSpecificCulture("zh-CN").NativeName;
            File.WriteAllText(Path.Combine(AppSettings.Directories.LanguageDir, "zh-CN.xaml"),
                GetXamlTemplate(GetXamlKeyValuePairTemplate(defUiStrings)));

            AvailableLangDic.Add(nativeName, "zh-CN");
        }

        SwitchToLang(string.IsNullOrWhiteSpace(locale) ? CultureInfo.CurrentUICulture.Name : locale);
    }

    private static void LoadI18NFile(FileInfo enumerateFile, Dictionary<string, string?> defUiStrings)
    {
        var fullText = File.ReadAllText(enumerateFile.FullName);
        var localeCode = Path.GetFileNameWithoutExtension(enumerateFile.Name);
        var nativeName = CultureInfo.CreateSpecificCulture(localeCode).NativeName;

        if (string.IsNullOrWhiteSpace(fullText)) // 读取文件，若空则新建
        {
            var defUiText = string.Join("\r\n",
                defUiStrings.Select(k => $@"    <sys:String x:Key=""{k.Key}"">{k.Value}</sys:String>"));
            var xamlTemplate = GetXamlTemplate(defUiText);
            File.WriteAllText(enumerateFile.FullName, xamlTemplate);
        }
        else
        {
            // 否则分析文件，和默认的做对比，如果少了相应的字段则补齐，然后排序写入
            var existKeyValuePairsFromFile = GetStringKeyValuePairs(fullText);

            var unspecifiedKvs =
                defUiStrings.Where(k => !existKeyValuePairsFromFile.ContainsKey(k.Key)).ToList(); // 是否缺少字段
            var shouldDelKvs =
                existKeyValuePairsFromFile.Where(k => !defUiStrings.ContainsKey(k.Key)).ToList(); // 是否缺少字段
            if (unspecifiedKvs.Count > 0 || shouldDelKvs.Count > 0)
            {
                foreach (var unspecifiedKv in unspecifiedKvs)
                {
                    if (unspecifiedKv.Value != null)
                    {
                        existKeyValuePairsFromFile.Add(unspecifiedKv.Key, unspecifiedKv.Value);
                    }
                }

                foreach (var shouldDelKv in shouldDelKvs)
                {
                    existKeyValuePairsFromFile.Remove(shouldDelKv.Key);
                }

                File.WriteAllText(enumerateFile.FullName,
                    GetXamlTemplate(GetXamlKeyValuePairTemplate(existKeyValuePairsFromFile)));
            }
        }

        AvailableLangDic.Add(nativeName, localeCode);
    }

    private static Dictionary<string, string?> GetStringKeyValuePairs(string fullText)
    {
        var keyValuePairs = new Dictionary<string, string?>();
        string? keyName = null;
        using var xamlReader = new XamlXmlReader(new StringReader(fullText));
        (int LineNumber, int LinePosition, Type Type)? currentObj = null;
        (int LineNumber, int LinePosition, string member)? currentMember = null;
        while (!xamlReader.IsEof)
        {
            var result = xamlReader.Read();
            if (!result) continue;
            if (xamlReader.NodeType == XamlNodeType.StartObject)
            {
                currentObj = (xamlReader.LineNumber, xamlReader.LinePosition,
                    xamlReader.Type.UnderlyingType);
            }
            else if (xamlReader.NodeType == XamlNodeType.EndObject)
            {
                currentObj = null;
            }
            else if (xamlReader.NodeType == XamlNodeType.EndMember)
            {
                currentMember = null;
            }

            if (currentObj?.Type == typeof(string))
            {
                if (xamlReader.NodeType == XamlNodeType.StartMember)
                {
                    currentMember = (xamlReader.LineNumber, xamlReader.LinePosition,
                        xamlReader.Member.Name);
                }
                else if (xamlReader.NodeType == XamlNodeType.Value &&
                         currentMember?.member == "Key")
                {
                    keyName = (string)xamlReader.Value;
                }
                else if (xamlReader.NodeType == XamlNodeType.Value &&
                         currentMember?.member == "_Initialization")
                {
                    if (keyName != null)
                    {
                        keyValuePairs.Add(keyName, (string)xamlReader.Value);
                    }
                }
            }
        }

        return keyValuePairs;
    }

    private static string GetXamlTemplate(string resDicStr)
    {
        var str = $@"<ResourceDictionary
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:sys=""clr-namespace:System;assembly=mscorlib"">
{resDicStr}
</ResourceDictionary>
";
        return str;
    }

    private static string GetXamlKeyValuePairTemplate(Dictionary<string, string?> existKeyValuePairsFromFile)
    {
        var kvStr = string.Join("\r\n", existKeyValuePairsFromFile
            .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
            .Where(k => k.Value != null)
            .Select(k => $@"    <sys:String x:Key=""{k.Key}"">{k.Value}</sys:String>"));
        return kvStr;
    }
}