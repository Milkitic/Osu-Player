using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Milky.OsuPlayer.ControlLibrary.Custom
{
    public class Loader : Control
    {
        static Loader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Loader), new FrameworkPropertyMetadata(typeof(Loader)));
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register("FillColor", typeof(Color), typeof(Loader),
                new UIPropertyMetadata(Color.FromArgb(255, 252, 89, 163), OnColorChanged)
        );

        //VS设计器属性支持
        //[Description("背景色"), Category("个性配置"), DefaultValue("#FF668899")]
        public Color FillColor
        {
            get => (Color)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public static readonly DependencyProperty RotateProperty =
            DependencyProperty.Register("Rotate", typeof(bool), typeof(Loader),
                new UIPropertyMetadata(false, null)
            );

        //VS设计器属性支持
        //[Description("背景色"), Category("个性配置"), DefaultValue(false)]
        public bool Rotate
        {
            get => (bool)GetValue(RotateProperty);
            set => SetValue(RotateProperty, value);
        }
    }
}
