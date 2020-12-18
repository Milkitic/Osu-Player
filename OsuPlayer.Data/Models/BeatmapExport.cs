using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapExport : BaseEntity
    {
        private bool _isValid = true;
        private long _size = 0;
        private DateTime? _creationTime;

        public Guid Id { get; set; }
        public string ExportPath { get; set; }

        [NotMapped]
        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (value == _isValid) return;
                _isValid = value;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public long Size
        {
            get => _size;
            set
            {
                if (value == _size) return;
                _size = value;
                OnPropertyChanged();
            }
        }

        [NotMapped]
        public DateTime? CreationTime
        {
            get => _creationTime;
            set
            {
                if (Nullable.Equals(value, _creationTime)) return;
                _creationTime = value;
                OnPropertyChanged();
            }
        }

        //fk
        public Beatmap Beatmap { get; set; }
        public string BeatmapId { get; set; }
    }
}