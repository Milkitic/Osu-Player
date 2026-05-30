using System.Windows;

namespace Milky.OsuPlayer.UiComponents.ButtonComponent
{
    public class MaxButton : SystemButton
    {
        private bool _isSigned;
        public MaxButton()
        {
            this.Click += OnClick;
        }

        private void OnClick(object sender, RoutedEventArgs args)
        {
            if (HostWindow != null && !_isSigned)
            {
                HostWindow.StateChanged += delegate
                {
                    if (HostWindow.WindowState == WindowState.Normal)
                    {
                        IsWindowMax = false;
                    }
                    else if (HostWindow.WindowState == WindowState.Maximized)
                    {
                        IsWindowMax = true;
                    }
                };

                _isSigned = true;
            }

            if (HostWindow != null)
            {
                if (HostWindow.WindowState == WindowState.Normal)
                {
                    HostWindow.WindowState = WindowState.Maximized;
                }
                else if (HostWindow.WindowState == WindowState.Maximized)
                {
                    HostWindow.WindowState = WindowState.Normal;
                }
            }
        }

        public bool IsWindowMax
        {
            get => (bool)GetValue(IsWindowMaxProperty);
            set => SetValue(IsWindowMaxProperty, value);
        }

        public static readonly DependencyProperty IsWindowMaxProperty =
            DependencyProperty.Register("IsWindowMax", typeof(bool), typeof(SystemButton), new PropertyMetadata(false));

    }
}