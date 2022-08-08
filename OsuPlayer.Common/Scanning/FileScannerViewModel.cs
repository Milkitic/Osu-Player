using Milki.OsuPlayer.Presentation.Interaction;

namespace Milki.OsuPlayer.Common.Scanning
{
    public class FileScannerViewModel : VmBase
    {
        private bool _isScanning;
        private bool _isCanceling;

        public bool IsScanning
        {
            get => _isScanning;
            internal set
            {
                _isScanning = value;
                OnPropertyChanged();
            }
        }

        public bool IsCanceling
        {
            get => _isCanceling;
            set
            {
                _isCanceling = value;
                OnPropertyChanged();
            }
        }
    }
}