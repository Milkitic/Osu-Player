using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// Interaction logic for ExceptionWindow.xaml
    /// </summary>
    public partial class ExceptionWindow : Window
    {
        public ExceptionWindow(Exception ex, bool isUiException)
        {
            InitializeComponent();
            var viewModel = (ExceptionWindowViewModel)DataContext;
            viewModel.Exception = ex;
            viewModel.IsUiException = isUiException;
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    class ExceptionWindowViewModel : ViewModelBase
    {
        private Exception _exception;
        private bool _isUiException;

        public Exception Exception
        {
            get => _exception;
            set
            {
                _exception = value;
                OnPropertyChanged();
            }
        }

        public bool IsUiException
        {
            get => _isUiException;
            set
            {
                _isUiException = value;
                OnPropertyChanged();
            }
        }
    }
}
