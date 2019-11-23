using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xaml;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;

namespace Milky.OsuPlayer.Utils
{
    public static class I18nUtil
    {
        private static ResourceDictionary _i18nDic;
        public static KeyValuePair<string, string> CurrentLocale { get; private set; }

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

        public static void LoadI18N()
        {
            var locale = AppSettings.Default.Interface.Locale;

            _i18nDic = App.Current.Resources.MergedDictionaries[0];
            var defLocale = _i18nDic.MergedDictionaries[0];
            var defUiStrings = defLocale.Keys.Cast<object>().ToDictionary(defKey => (string)defKey, defKey => (string)defLocale[defKey]);

            foreach (var enumerateFile in Util.EnumerateFiles(Domain.LangPath, ".xaml"))
            {
                try
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
                        // 否则分析文件，和默认的做对比，如果少了相应的字段则补齐
                        using (var xamlReader = new XamlXmlReader(new StringReader(fullText)))
                        {
                            string keyName = null;

                            (int LineNumber, int LinePosition, Type Type)? currentObj = null;
                            (int LineNumber, int LinePosition, string member)? currentMember = null;
                            int firstObjLine = 0;
                            var kvs = new Dictionary<string, string>();
                            while (!xamlReader.IsEof)
                            {
                                var result = xamlReader.Read();
                                if (!result) continue;
                                if (xamlReader.NodeType == XamlNodeType.StartObject)
                                {
                                    currentObj = (xamlReader.LineNumber, xamlReader.LinePosition, xamlReader.Type.UnderlyingType);
                                }
                                else if (xamlReader.NodeType == XamlNodeType.EndObject)
                                {
                                    currentObj = null;
                                }
                                else if (xamlReader.NodeType == XamlNodeType.EndMember)
                                {
                                    currentMember = null;
                                }

                                //if (currentObj?.Type == typeof(ResourceDictionary))
                                //{
                                //    if (xamlReader.NodeType == XamlNodeType.StartMember)
                                //    {
                                //        currentMember = (xamlReader.LineNumber, xamlReader.LinePosition, xamlReader.Member.Name);
                                //    }
                                //    else if (xamlReader.NodeType == XamlNodeType.Value && currentMember?.member == "Name")
                                //    {
                                //        langName = (string)xamlReader.Value;
                                //    }
                                //}
                                //else 
                                if (currentObj?.Type == typeof(string))
                                {
                                    if (xamlReader.NodeType == XamlNodeType.StartMember)
                                    {
                                        currentMember = (xamlReader.LineNumber, xamlReader.LinePosition, xamlReader.Member.Name);
                                    }
                                    else if (xamlReader.NodeType == XamlNodeType.Value && currentMember?.member == "Key")
                                    {
                                        keyName = (string)xamlReader.Value;
                                    }
                                    else if (xamlReader.NodeType == XamlNodeType.Value && currentMember?.member == "_Initialization")
                                    {
                                        if (kvs.Count == 0)
                                        {
                                            firstObjLine = xamlReader.LineNumber;
                                        }

                                        kvs.Add(keyName, (string)xamlReader.Value);
                                    }
                                }

                            }

                            var unspecifiedKvs = defUiStrings.Where(k => !kvs.ContainsKey(k.Key)).ToList(); // 是否缺少字段
                            if (unspecifiedKvs.Count > 0)
                            {
                                var kvStr = string.Join("\r\n",
                                    unspecifiedKvs.Select(k => $@"    <sys:String x:Key=""{k.Key}"">{k.Value}</sys:String>"));

                                using (var sr = new StringReader(fullText))
                                using (var sw = new StreamWriter(enumerateFile.FullName))
                                {
                                    int i = 1;
                                    string line = sr.ReadLine();
                                    while (line != null)
                                    {
                                        if (i == firstObjLine)
                                        {
                                            sw.WriteLine(kvStr);
                                        }

                                        sw.WriteLine(line);
                                        line = sr.ReadLine();
                                        i++;
                                    }
                                }
                            }

                        }
                    }

                    AvailableLangDic.Add(nativeName, localeCode);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            if (!AvailableLangDic.ContainsValue("zh-CN"))
            {
                var chiUiText = string.Join("\r\n",
                    defUiStrings.Select(k => $@"    <sys:String x:Key=""{k.Key}"">{k.Value}</sys:String>"));
                var nativeName = CultureInfo.CreateSpecificCulture("zh-CN").NativeName;
                File.WriteAllText(Path.Combine(Domain.LangPath, "zh-CN.xaml"), GetXamlTemplate(chiUiText));

                AvailableLangDic.Add(nativeName, "zh-CN");
            }

            SwitchToLang(string.IsNullOrWhiteSpace(locale) ? CultureInfo.CurrentCulture.Name : locale);
        }

        public static void SwitchToLang(string locale)
        {
            if (!AvailableLangDic.ContainsValue(locale))
            {
                locale = "zh-CN";
            }

            ResourceDictionary langRd;
            using (var s = new FileStream(Path.Combine(Domain.LangPath, $"{locale}.xaml"), FileMode.Open))
            {
                langRd = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(s);
            }

            if (langRd == null) return;
            if (_i18nDic.MergedDictionaries.Count > 0)
            {
                // 如果已使用其他语言,先清空
                _i18nDic.MergedDictionaries.Clear();
            }

            _i18nDic.MergedDictionaries.Add(langRd);
            CurrentLocale = AvailableLangDic.First(k => k.Value == locale);
        }

        public static Dictionary<string, string> AvailableLangDic { get; set; } = new Dictionary<string, string>();
    }
}
