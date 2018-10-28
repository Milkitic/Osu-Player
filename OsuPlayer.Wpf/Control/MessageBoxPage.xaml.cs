using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Milkitic.OsuPlayer.Control
{
    /// <summary>
    /// MessageBoxPage.xaml 的交互逻辑
    /// </summary>
    public partial class MessageBoxPage : Page
    {
        private readonly Action _confirmAction;
        private readonly Action _cancelAction;
        private readonly bool _dialogMode;
        public bool? DialogResult { get; set; }
        public bool IsClosed;
        public MessageBoxPage(string title, string content, Action cancelAction)
        {
            _cancelAction = cancelAction;
            _dialogMode = true;
            InitializeComponent();
            LblTitle.Content = title;
            LblMessage.Content = content;
        }

        public MessageBoxPage(string title, string content, Action confirmAction, Action cancelAction)
        {
            _confirmAction = confirmAction;
            _cancelAction = cancelAction;
            _dialogMode = false;
            InitializeComponent();
            LblTitle.Content = title;
            LblMessage.Content = content;
        }

        public void Dispose()
        {
            _cancelAction();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!_dialogMode)
            {
                _confirmAction();
                Dispose();
            }
            else
            {
                DialogResult = true;
                IsClosed = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_dialogMode)
                Dispose();
            else
            {
                DialogResult = null;
                IsClosed = true;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BtnOk.Focus();
        }
        Point pos = new Point();
        private void DockPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            pos = e.GetPosition(null);
            BoxGrid.CaptureMouse();
        }

        private void DockPanel_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double dx = (e.GetPosition(null).X - pos.X) * 2 + BoxGrid.Margin.Left;
                double dy = (e.GetPosition(null).Y - pos.Y) *2 + BoxGrid.Margin.Top;
                BoxGrid.Margin = new Thickness(dx, dy, 0, 0);
                double dx1 = (e.GetPosition(null).X - pos.X) *2 + Parallel.Margin.Left;
                double dy1 = (e.GetPosition(null).Y - pos.Y) *2 + Parallel.Margin.Top;
                Parallel.Margin = new Thickness(dx, dy, 0, 0);
                pos = e.GetPosition(null);
            }
        }

        private void DockPanel_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BoxGrid.ReleaseMouseCapture();
        }
    }
}

