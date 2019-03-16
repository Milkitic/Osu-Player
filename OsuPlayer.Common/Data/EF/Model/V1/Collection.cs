using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Milky.WpfApi;
using Newtonsoft.Json;

namespace Milky.OsuPlayer.Common.Data.EF.Model.V1
{
    [Table("collection")]
    public class Collection : ViewModelBase
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
            LockedInt = locked ? 1 : 0;
            Index = index;
            ImagePath = imagePath;
            Description = description;
        }

        [Required, Column("id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        [Required, Column("name")]
        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        [Column("locked")]
        [JsonProperty("locked")]
        public int LockedInt { get; set; }

        [Column("index")]
        [JsonProperty("index")]
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
            }
        }

        [Column("imagePath")]
        [JsonProperty("imagePath")]
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                OnPropertyChanged();
            }
        }

        [Column("description")]
        [JsonProperty("description")]
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        [Required, Column("createTime")]
        [JsonProperty("createTime")]
        public DateTime CreateTime
        {
            get => _createTime;
            set
            {
                _createTime = value;
                OnPropertyChanged();
            }
        }

        public bool Locked => LockedInt == 1;
    }
}