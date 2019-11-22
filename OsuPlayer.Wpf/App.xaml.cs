using Microsoft.Win32;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.I18N;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Common.Scanning;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xaml;
using Milky.OsuPlayer.Control.Notification;

#if !DEBUG
using Sentry;
#endif

namespace Milky.OsuPlayer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
#if !DEBUG
            SentrySdk.Init("https://1fe13baa86284da5a0a70efa9750650e:fcbd468d43f94fb1b43af424517ec00b@sentry.io/1412154");
#endif
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
            StartupConfig.Startup();

            Services.TryAddInstance(new UiMetadata());
            var playerList = new PlayerList { PlayerMode = AppSettings.Default.Play.PlayerMode };
            Services.TryAddInstance(playerList);
            Services.TryAddInstance(new OsuDbInst());
            Services.TryAddInstance(new PlayersInst());
            Services.TryAddInstance(new LyricsInst());
            Services.TryAddInstance(new Updater());
            Services.TryAddInstance(new OsuFileScanner());

            Services.Get<LyricsInst>().ReloadLyricProvider();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        private static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
#if !DEBUG
                SentrySdk.CaptureException(ex);
#endif
                var exceptionWindow = new ExceptionWindow(ex, false);
                exceptionWindow.ShowDialog();
            }

            if (!e.IsTerminating)
            {
                return;
            }

            Environment.Exit(1);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
#if !DEBUG
            SentrySdk.CaptureException(e.Exception);
#endif
            var exceptionWindow = new ExceptionWindow(e.Exception, true);
            var val = exceptionWindow.ShowDialog();
            e.Handled = val != true;
            if (val == true)
            {
                Environment.Exit(1);
            }
        }

        public static ObservableCollection<NotificationOption> NotificationList { get; set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            LoadI18N();
        }

        private void LoadI18N()
        {
            var locale = AppSettings.Default.Interface.Locale;
            var dic = this.Resources.MergedDictionaries[0];
            var def = dic.MergedDictionaries[0];
            var kv = def.Keys.Cast<object>().ToDictionary(defKey => (string)defKey, defKey => (string)def[defKey]);
            foreach (var enumerateFile in Util.EnumerateFiles(Domain.LangPath, ".xaml"))
            {
                try
                {
                    var allText = File.ReadAllText(enumerateFile.FullName);
                    if (string.IsNullOrWhiteSpace(allText))
                    {
                        var kvStr = string.Join("\r\n",
                            kv.Select(k => $@"    <sys:String x:Key=""{k.Key}"">{k.Value}</sys:String>"));
                        var str = $@"<ResourceDictionary
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:sys=""clr-namespace:System;assembly=mscorlib""
    x:Name=""UiLang{Path.GetFileNameWithoutExtension(enumerateFile.Name)}"">
{kvStr}
</ResourceDictionary>
";
                        File.WriteAllText(enumerateFile.FullName, str);
                    }
                    else
                    {
                        using (var sr = new StringReader(allText))
                        using (var xr = new XamlXmlReader(sr))
                        {
                            string langName = Path.GetFileNameWithoutExtension(enumerateFile.Name);
                            string keyName = null;
                            (int LineNumber, int LinePosition, Type Type)? currentObj = null;
                            (int LineNumber, int LinePosition, string member)? currentMember = null;
                            int firstLine = 0;
                            var kvs = new Dictionary<string, string>();
                            while (!xr.IsEof)
                            {
                                var result = xr.Read();
                                if (xr.NodeType == XamlNodeType.StartObject)
                                {
                                    currentObj = (xr.LineNumber, xr.LinePosition, xr.Type.UnderlyingType);
                                }
                                else if (xr.NodeType == XamlNodeType.EndObject)
                                {
                                    currentObj = null;
                                }
                                else if (xr.NodeType == XamlNodeType.EndMember)
                                {
                                    currentMember = null;
                                }

                                if (currentObj?.Type == typeof(ResourceDictionary))
                                {
                                    if (xr.NodeType == XamlNodeType.StartMember)
                                    {
                                        currentMember = (xr.LineNumber, xr.LinePosition, xr.Member.Name);
                                    }
                                    else if (xr.NodeType == XamlNodeType.Value && currentMember?.member == "Name")
                                    {
                                        langName = (string)xr.Value;
                                    }
                                }
                                else if (currentObj?.Type == typeof(string))
                                {
                                    if (xr.NodeType == XamlNodeType.StartMember)
                                    {
                                        currentMember = (xr.LineNumber, xr.LinePosition, xr.Member.Name);
                                    }
                                    else if (xr.NodeType == XamlNodeType.Value && currentMember?.member == "Key")
                                    {
                                        keyName = (string)xr.Value;
                                    }
                                    else if (xr.NodeType == XamlNodeType.Value && currentMember?.member == "_Initialization")
                                    {
                                        if (kvs.Count == 0)
                                        {
                                            firstLine = xr.LineNumber;
                                        }

                                        kvs.Add(keyName, (string)xr.Value);
                                    }
                                }

                            }
                        }
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
