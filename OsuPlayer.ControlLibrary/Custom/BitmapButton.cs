using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.ControlLibrary.Custom
{
    public class BitmapButton : Button
    {
        static BitmapButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BitmapButton), new FrameworkPropertyMetadata(typeof(BitmapButton)));
        }
    }
}
