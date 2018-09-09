using Milkitic.OsuPlayer.Models;
using Milkitic.OsuPlayer.Utils;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Milkitic.OsuPlayer.Winforms;

namespace Milkitic.OsuPlayer
{
    static class Core
    {
        public static Config Config { get; set; }

        [STAThread]
        static void Main()
        {
            var file = Domain.ConfigFile;
            if (!File.Exists(file))
            {
                CreateConfig(file);
            }
            else
            {
                try
                {
                    Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(file));
                }
                catch (JsonException e)
                {
                    var result = MessageBox.Show(@"载入配置文件时失败，用默认配置覆盖继续打开吗？\r\n" + e.Message,
                        AppDomain.CurrentDomain.FriendlyName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        CreateConfig(file);
                    }
                    else
                        return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RenderForm());

            SaveConfig(file);
        }

        private static void SaveConfig(string file)
        {
            File.WriteAllText(file, JsonConvert.SerializeObject(Config));
        }

        private static void CreateConfig(string file)
        {
            Config = new Config();
            File.WriteAllText(file, JsonConvert.SerializeObject(Config));
        }
    }
}
