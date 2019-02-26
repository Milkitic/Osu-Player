using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.IO;
using System.Linq;
using System.Reflection;
using Milky.OsuPlayer.Migrations;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Data
{
    [Table("map_info")]
    public class MapInfo
    {
        public MapInfo() { }

        public MapInfo(string id, string version, string folderName, int offset, DateTime? lastPlayTime,
            string exportFile = null, DateTime? addTime = null)
        {
            Id = id;
            Version = version;
            FolderName = folderName;
            Offset = offset;
            LastPlayTime = lastPlayTime;
            AddTime = addTime;
            if (exportFile != null) ExportFile = exportFile;
        }

        [Required, Column("id")]
        public string Id { get; set; }
        [Required, Column("version")]
        public string Version { get; set; }
        [Required, Column("folder")]
        public string FolderName { get; set; }
        [Column("offset")]
        public int Offset { get; set; }
        [Column("lastPlayTime")]
        public DateTime? LastPlayTime { get; set; }
        [Column("exportFile")]
        public string ExportFile { get; set; }

        //Extension
        public DateTime? AddTime { get; }

    }


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
        public string Id { get; set; }

        [Required, Column("name")]
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
        public int LockedInt { get; set; }

        [Column("index")]
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


    [Table("collection_relation")]
    public class CollectionRelation
    {
        public CollectionRelation() { }

        public CollectionRelation(string id, string collectionId, string mapId)
        {
            Id = id;
            CollectionId = collectionId;
            MapId = mapId;
            AddTime = DateTime.Now;
        }

        [Required, Column("id")]
        public string Id { get; set; }
        [Required, Column("collectionId")]
        public string CollectionId { get; set; }
        [Required, Column("mapId")]
        public string MapId { get; set; }
        [Column("addTime")]
        public DateTime? AddTime { get; set; }
    }

    public class ApplicationDbContext : DbContext
    {
        static ApplicationDbContext()
        {
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<ApplicationDbContext, MigrationConfiguration>(true));
        }

        public DbSet<MapInfo> MapInfos { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<CollectionRelation> Relations { get; set; }

        public ApplicationDbContext() : base("name=sqlite")
        {
            //Database.Initialize(false);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Conventions
            //    .Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
            //base.OnModelCreating(modelBuilder);
        }
    }
}