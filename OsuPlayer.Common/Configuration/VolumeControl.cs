using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class VolumeControl : INotifyPropertyChanged
    {
        private float _main = 0.8f;
        private float _bgm = 1;
        private float _hs = 0.9f;

        public float Main
        {
            get => _main;
            set
            {
                SetValue(ref _main, value);
                OnPropertyChanged();
            }
        }

        public float Music
        {
            get => _bgm; set
            {
                SetValue(ref _bgm, value);
                OnPropertyChanged();
            }
        }
        public float Hitsound
        {
            get => _hs; set
            {
                SetValue(ref _hs, value);
                OnPropertyChanged();
            }
        }

        private static void SetValue(ref float source, float value) => source = value < 0 ? 0 : (value > 1 ? 1 : value);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}