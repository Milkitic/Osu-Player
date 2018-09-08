using Milkitic.OsuPlayer.Models;
using Milkitic.OsuPlayer.Utils;
using NAudio.Wave;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PlayerMain());
        }

        private static void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {

        }
    }
}
