using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Milky.OsuPlayer.Annotations;

namespace Milky.OsuPlayer.Models
{
    internal class StoryboardDataModel : INotifyPropertyChanged
    {
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Folder { get; set; }

        public string ThumbPath { get; set; }
        public string ThumbVideoPath { get; set; }
        public string OsbSize { get; set; }
        public long Length { get; set; }

        public bool DiffHasStoryboardOnly { get; set; }

        public List<string> ContainsVersions { get; set; } = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}