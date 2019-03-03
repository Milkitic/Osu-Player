using System.Windows;
using System.Windows.Controls;

namespace Loaders
{
    public class ImgurLoader : Control
    {
        #region IsIndeterminate
        public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(
            "IsIndeterminate",
            typeof(bool),
            typeof(ImgurLoader),
            new PropertyMetadata(default(bool))
        );

        public bool IsIndeterminate
        {
            get { return (bool)GetValue(IsIndeterminateProperty); }
            set { SetValue(IsIndeterminateProperty, value); }
        }
        #endregion

        #region RingsThickness
        public static readonly DependencyProperty RingsThicknessProperty = DependencyProperty.Register(
            "RingsThickness",
            typeof(double),
            typeof(ImgurLoader),
            new PropertyMetadata(default(double))
        );

        public double RingsThickness
        {
            get { return (double)GetValue(RingsThicknessProperty); }
            set { SetValue(RingsThicknessProperty, value); }
        }
        #endregion


        static ImgurLoader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImgurLoader), new FrameworkPropertyMetadata(typeof(ImgurLoader)));
        }
    }
}
