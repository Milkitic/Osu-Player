using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OSharp.Beatmap;
using PlayerTest.Player;

namespace PlayerTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var path = "E:\\milkitic\\others\\Aeventyr\\Grand Thaw - Aeventyr (bms2osu) [lv.12 MX].osu";
            var sw = Stopwatch.StartNew();
            var osuFile = await OsuFile.ReadFromFileAsync(path);
            Console.WriteLine(sw.ElapsedMilliseconds);
            sw.Restart();
            var g = new OsuMixPlayer(osuFile, System.IO.Path.GetDirectoryName(path));

            await g.Initialize();
            Console.WriteLine(sw.ElapsedMilliseconds);
            sw.Reset();
        }
    }
}
