using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Expression.Media;

namespace Loaders
{
    public class ChromeLoader : Control
    {
        #region IsIndeterminate
        public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(
            "IsIndeterminate",
            typeof(bool),
            typeof(ChromeLoader),
            new PropertyMetadata(default(bool))
        );

        public bool IsIndeterminate
        {
            get { return (bool)GetValue(IsIndeterminateProperty); }
            set { SetValue(IsIndeterminateProperty, value); }
        }
        #endregion

        #region ThicknessUnit
        public static readonly DependencyProperty ThicknessUnitProperty = DependencyProperty.Register(
            "ThicknessUnit",
            typeof(UnitType),
            typeof(ChromeLoader),
            new PropertyMetadata(default(UnitType))
        );

        public UnitType ThicknessUnit
        {
            get { return (UnitType)GetValue(ThicknessUnitProperty); }
            set { SetValue(ThicknessUnitProperty, value); }
        }
        #endregion

        #region Thickness
        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
            "Thickness",
            typeof(double),
            typeof(ChromeLoader),
            new PropertyMetadata(default(double))
        );

        public double Thickness
        {
            get { return (double)GetValue(ThicknessProperty); }
            set { SetValue(ThicknessProperty, value); }
        }
        #endregion

        #region Fill
        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            "Fill", typeof (Brush), 
            typeof (ChromeLoader), 
            new PropertyMetadata(default(Brush))
        );

        public Brush Fill
        {
            get { return (Brush) GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }
        #endregion


        static ChromeLoader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChromeLoader), new FrameworkPropertyMetadata(typeof(ChromeLoader)));
        }
    }
}
