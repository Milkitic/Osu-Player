using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Milkitic.OsuPlayer.Control
{
    public class PageBox
    {
        private readonly Panel _panel;
        private readonly string _name;

        public PageBox(Grid panel, string name)
        {
            _panel = panel;
            _name = name;

            if (_panel.FindName(_name) is Frame) return;
            Frame frame = new Frame
            {
                NavigationUIVisibility = NavigationUIVisibility.Hidden,
            };
            panel.Children.Add(frame);
            frame.SetValue(Grid.RowProperty, 0);
            frame.SetValue(Grid.ColumnProperty, 0);
            frame.SetValue(Grid.RowSpanProperty, panel.RowDefinitions.Count == 0 ? 1 : panel.RowDefinitions.Count); //设置按钮所在Grid控件的行
            frame.SetValue(Grid.ColumnSpanProperty, panel.ColumnDefinitions.Count == 0 ? 1 : panel.RowDefinitions.Count); //设置按钮所在Grid控件的行
            panel.RegisterName(name, frame);

        }

        public void Show(string title, string content, Action callBack)
        {
            if (_panel.FindName(_name) is Frame frame)
                frame.Navigate(new MessageBoxPage(title, content, callBack, () => frame.Navigate(null)));
        }
        public async Task<bool?> ShowDialog(string title, string content)
        {
            if (_panel.FindName(_name) is Frame frame)
            {
                var page = new MessageBoxPage(title, content, () => frame.Navigate(null));
                frame.Navigate(page);
                while (!page.IsClosed)
                {
                    await Task.Delay(1);
                }

                bool? b = page.DialogResult;
                page.Dispose();
                return b;
            }

            return null;
        }
    }
}
