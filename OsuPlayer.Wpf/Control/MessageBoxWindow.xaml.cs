using DMSkin.WPF;
using System;
using System.Windows;
using System.Windows.Media;

namespace Milky.OsuPlayer.Control
{
    /// <summary>
    /// MessageBoxWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MessageBoxWindow : DMSkinSimpleWindow
    {
        private MessageBoxImage _icon;
        public MessageBoxResult MessageBoxResult { get; set; }

        public MessageBoxWindow(string messageBoxText)
        {
            InitializeComponent();
            LblMessage.Content = messageBoxText;
        }

        public MessageBoxWindow(string messageBoxText, string caption) : this(messageBoxText)
        {
            Title = caption;
        }

        public MessageBoxWindow(string messageBoxText, string caption, MessageBoxButton button)
            : this(messageBoxText, caption)
        {
            switch (button)
            {
                case MessageBoxButton.OK:
                    BtnYes.Visibility = Visibility.Collapsed;
                    BtnNo.Visibility = Visibility.Collapsed;
                    BtnCancel.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.OKCancel:
                    BtnYes.Visibility = Visibility.Collapsed;
                    BtnNo.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.YesNoCancel:
                    BtnOk.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.YesNo:
                    BtnOk.Visibility = Visibility.Collapsed;
                    BtnCancel.Visibility = Visibility.Collapsed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, null);
            }
        }

        public MessageBoxWindow(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
            : this(messageBoxText, caption, button)
        {
            _icon = icon;
            switch (icon)
            {
                case MessageBoxImage.None:
                    TitleBarArea.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    BtnClose.DMSystemButtonForeground = new SolidColorBrush(Color.FromArgb(224, 128, 128, 128));
                    LblTitle.Foreground = new SolidColorBrush(Color.FromRgb(48, 48, 48));
                    break;
                case MessageBoxImage.Error:
                    ErrorIcon.Visibility = Visibility.Visible;
                    TitleBarArea.Background = new SolidColorBrush(Color.FromRgb(242, 51, 63));
                    break;
                case MessageBoxImage.Question:
                    QuestionIcon.Visibility = Visibility.Visible;
                    TitleBarArea.Background = new SolidColorBrush(Color.FromRgb(75, 154, 254));
                    break;
                case MessageBoxImage.Exclamation:
                    WarnIcon.Visibility = Visibility.Visible;
                    TitleBarArea.Background = new SolidColorBrush(Color.FromRgb(255, 170, 53));
                    break;
                case MessageBoxImage.Information:
                    InfoIcon.Visibility = Visibility.Visible;
                    TitleBarArea.Background = new SolidColorBrush(Color.FromRgb(78, 192, 69));
                    break;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult = MessageBoxResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult = MessageBoxResult.Cancel;
            Close();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult = MessageBoxResult.Yes;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult = MessageBoxResult.No;
            Close();
        }

        private void DMSystemCloseButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult = MessageBoxResult.Cancel;
        }

        private void MsgBoxWindow_Loaded(object sender, RoutedEventArgs e)
        {
            switch (_icon)
            {
                case MessageBoxImage.Hand:
                    System.Media.SystemSounds.Hand.Play();
                    break;
                case MessageBoxImage.Question:
                    System.Media.SystemSounds.Question.Play();
                    break;
                case MessageBoxImage.Exclamation:
                    System.Media.SystemSounds.Exclamation.Play();
                    break;
                case MessageBoxImage.Asterisk:
                    System.Media.SystemSounds.Asterisk.Play();
                    break;
            }
        }
    }
}
