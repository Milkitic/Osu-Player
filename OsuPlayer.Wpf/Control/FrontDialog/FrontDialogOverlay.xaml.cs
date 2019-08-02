using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Control.FrontDialog
{
    /// <summary>
    /// FrontDialogOverlay.xaml 的交互逻辑
    /// </summary>
    public partial class FrontDialogOverlay : UserControl
    {
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set
            {
                SetValue(TitleProperty, value);
                Header.Content = value;
            }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title",
                typeof(string),
                typeof(FrontDialogOverlay),
                new PropertyMetadata(""));

        public double BoxWidth
        {
            get => (double)GetValue(BoxWidthProperty);
            set
            {
                SetValue(BoxWidthProperty, value);
                BoxGrid.Width = value;
            }
        }

        public static readonly DependencyProperty BoxWidthProperty =
            DependencyProperty.Register(
                "BoxWidth",
                typeof(double),
                typeof(FrontDialogOverlay));

        public double BoxHeight
        {
            get => (double)GetValue(BoxHeightProperty);
            set
            {
                SetValue(BoxHeightProperty, value);
                BoxGrid.Height = value;
            }
        }

        public static readonly DependencyProperty BoxHeightProperty =
            DependencyProperty.Register(
                "BoxHeight",
                typeof(double),
                typeof(FrontDialogOverlay));

        public object BodyContent
        {
            get => (object)GetValue(BodyContentProperty);
            set
            {
                SetValue(BodyContentProperty, value);
                Body.Content = value;
            }
        }

        public static readonly DependencyProperty BodyContentProperty =
            DependencyProperty.Register(
                "BodyContent",
                typeof(object),
                typeof(FrontDialogOverlay),
                new PropertyMetadata(null));

        public FrontDialogOverlay()
        {
            InitializeComponent();
        }
    }
}