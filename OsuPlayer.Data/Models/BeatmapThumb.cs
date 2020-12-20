using Milky.OsuPlayer.Presentation.Annotations;
using System;
using System.ComponentModel.DataAnnotations;

namespace Milky.OsuPlayer.Data.Models
{
    public class BeatmapThumb : BaseEntity
    {
        private string _thumbPath;
        [Key]
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
        public byte[] BeatmapId { get; set; }
    }
}