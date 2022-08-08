using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Milky.OsuPlayer.Presentation.Annotations;

namespace Milki.OsuPlayer.Data.Models
{
    public class BaseEntity : INotifyPropertyChanged
    {
        private DateTime _createTime;
        private DateTime _updateTime;

        public DateTime CreateTime
        {
            get => _createTime;
            set
            {
                if (value == _createTime) return;
                _createTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime UpdateTime
        {
            get => _updateTime;
            set
            {
                if (value == _updateTime) return;
                _updateTime = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}