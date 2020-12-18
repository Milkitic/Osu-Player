using Milky.OsuPlayer.Presentation.Annotations;
using System;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapThumb : BaseEntity
    {
        private string _thumbPath;
        public Guid Id { get; set; }

        public string ThumbPath
        {
            get => _thumbPath;
            set
            {
                if (value == _thumbPath) return;
                _thumbPath = value;
                OnPropertyChanged();
            }
        }

        public string VideoPath { get; set; }

        //fk
        [CanBeNull] public BeatmapStoryboard BeatmapStoryboard { get; set; }
        public Guid? BeatmapStoryboardId { get; set; }
        public Beatmap Beatmap { get; set; }
        public string BeatmapId { get; set; }
    }
}