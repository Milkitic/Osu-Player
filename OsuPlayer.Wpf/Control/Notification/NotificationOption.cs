using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Control.Notification
{
    public class NotificationOption : INotifyPropertyChanged
    {
        #region Notify property

        private ControlTemplate _iconTemplate;
        private string _title = "Title";
        private string _content = "This is your content here";
        private TimeSpan _fadeoutTime;
        private NotificationType _notificationType;

        public ControlTemplate IconTemplate
        {
            get => _iconTemplate;
            set
            {
                _iconTemplate = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan FadeoutTime
        {
            get => _fadeoutTime;
            set
            {
                _fadeoutTime = value;
                OnPropertyChanged();
            }
        }

        public NotificationType NotificationType
        {
            get => _notificationType;
            set
            {
                _notificationType = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public string NotificationTypeString => NotificationType.ToString();

        public bool IsEmpty => string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Content) && IconTemplate == null;

        public Action YesCallback { get; set; }
        public Action NoCallback { get; set; }
    }
}