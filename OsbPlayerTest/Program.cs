using LibOsb;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OsbPlayerTest
{
    static class Program
    {

        public static readonly FileInfo Fi =
            new FileInfo(
                @"D:\Program Files (x86)\osu!\Songs\584787 Yuiko Ohara - Hoshi o Tadoreba\Yuiko Ohara - Hoshi o Tadoreba (Yumeno Himiko).osb");
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            var text = File.ReadAllText(Fi.FullName);
            ElementGroup sb = ElementGroup.Parse(text, 0);
            File.WriteAllText("sb.osb", sb.ToString());
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RenderForm(sb));
        }
    }
}
