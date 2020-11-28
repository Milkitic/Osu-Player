using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace Milky.OsuPlayer.Data.Models
{
    public class Collection : BaseEntity
    {
        private string _name;
        private int _index;
        private string _imagePath;
        private string _description;

        public Guid Id { get; set; }
        public bool IsLocked { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public int Index
        {
            get => _index;
            set
            {
                if (value == _index) return;
                _index = value;
                OnPropertyChanged();
            }
        }

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (value == _imagePath) return;
                _imagePath = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public List<Beatmap> Beatmaps { get; set; }
    }
}