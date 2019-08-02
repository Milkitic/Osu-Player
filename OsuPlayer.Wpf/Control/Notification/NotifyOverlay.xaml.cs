using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Control.Notification
{
    /// <summary>
    /// NotifyOverlay.xaml 的交互逻辑
    /// </summary>
    public partial class NotifyOverlay : UserControl
    {
        private ObservableCollection<NotificationOption> _itemsSource;

        public NotifyOverlay()
        {
            InitializeComponent();
        }

        public ObservableCollection<NotificationOption> ItemsSource
        {
            get => _itemsSource;
            set
            {
                if (_itemsSource != null)
                {
                    _itemsSource.CollectionChanged -= Oc_CollectionChanged;
                }

                _itemsSource = value;
                _itemsSource.CollectionChanged += Oc_CollectionChanged;
            }
        }

        private void Oc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var newItem in e.NewItems)
                {
                    TriggerIn((NotificationOption)newItem);
                }
            }
        }

        private void TriggerIn(NotificationOption newItem)
        {
            var sb = new NotifyControl(newItem, _itemsSource);
            NotifyStack.Children.Add(sb);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
