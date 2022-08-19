using System.Windows;
using System.Windows.Media;

namespace Milki.OsuPlayer.UiComponents.LoaderComponent;

public class Loader : System.Windows.Controls.Control
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

    //public static readonly DependencyProperty RotateProperty =
    //    DependencyProperty.Register("Rotate", typeof(bool), typeof(Loader),
    //        new UIPropertyMetadata(false, null)
    //    );

    ////VS设计器属性支持
    ////[Description("背景色"), Category("个性配置"), DefaultValue(false)]
    //public bool Rotate
    //{
    //    get => (bool)GetValue(RotateProperty);
    //    set => SetValue(RotateProperty, value);
    //}
}