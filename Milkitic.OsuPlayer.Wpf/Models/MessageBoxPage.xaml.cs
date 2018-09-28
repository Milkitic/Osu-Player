using System;
using System.Windows;
using System.Windows.Controls;

namespace Milkitic.OsuPlayer.Wpf.Models
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
    }
}

