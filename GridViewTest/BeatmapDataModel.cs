using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GridViewTest
{
    public class BeatmapDataModel : INotifyPropertyChanged
    {
        private string _artistUnicode;
        private string _titleUnicode;
        private string _creator;
        private string _version;
        private double _stars;

        public string ArtistUnicode
        {
            get => _artistUnicode;
            set
            {
                _artistUnicode = value;
                OnPropertyChanged();
            }
        }

        public string TitleUnicode
        {
            get => _titleUnicode;
            set
            {
                _titleUnicode = value;
                OnPropertyChanged();
            }
        }

        public string Creator
        {
            get => _creator;
            set
            {
                _creator = value;
                OnPropertyChanged();
            }
        } 

        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged();
            }
        } 

        public double Stars
        {
            get => _stars;
            set
            {
                _stars = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}