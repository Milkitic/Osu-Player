using System;
using Dapper.FluentMap.Mapping;
using Milky.OsuPlayer.Presentation.Interaction;

namespace Milky.OsuPlayer.Data.Models
{
    public class CollectionMap : EntityMap<Collection>
    {
        public CollectionMap()
        {
            Map(p => p.Id).ToColumn("id");
            Map(p => p.Name).ToColumn("name");
            Map(p => p.Locked).ToColumn("locked");
            Map(p => p.Index).ToColumn("index");
            Map(p => p.ImagePath).ToColumn("imagePath");
            Map(p => p.Description).ToColumn("description");
            Map(p => p.CreateTime).ToColumn("createTime");
        }
    }

    public class Collection : VmBase
    {
        private string _imagePath;
        private string _description;
        private string _name;
        private DateTime _createTime;
        private int _index;
        public Collection() { }

        public Collection(string id, string name, bool locked, int index, string imagePath = null, string description = null)
        {
            Id = id;
            Name = name;
            Locked = locked ? 1 : 0;
            Index = index;
            ImagePath = imagePath;
            Description = description;
        }

        public string Id { get; set; }
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public int Locked { get; set; }
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
            }
        }

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreateTime
        {
            get => _createTime;
            set
            {
                _createTime = value;
                OnPropertyChanged();
            }
        }

        public bool LockedBool => Locked == 1;
    }
}