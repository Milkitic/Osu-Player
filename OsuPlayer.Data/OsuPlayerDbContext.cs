using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Data
{
    public class OsuPlayerDbContext : DbContext
    {
        public static string DefaultDatabasePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "player.db");
        public static string DefaultConnectionString => $"Data Source={DefaultDatabasePath}";

        public DbSet<Beatmap> Beatmaps { get; set; }
        public DbSet<BeatmapSettings> BeatmapSettings { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<CollectionRelation> CollectionRelations { get; set; }
        public DbSet<MapThumb> MapThumbs { get; set; }
        public DbSet<StoryboardInfo> StoryboardInfos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(DefaultConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureBeatmap(modelBuilder);
            ConfigureBeatmapSettings(modelBuilder);
            ConfigureCollection(modelBuilder);
            ConfigureCollectionRelation(modelBuilder);
            ConfigureMapThumb(modelBuilder);
            ConfigureStoryboardInfo(modelBuilder);
        }

        private static void ConfigureBeatmap(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Beatmap>();

            entity.ToTable("beatmap");
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => new { k.FolderName, k.Version, k.InOwnDb })
                .HasDatabaseName("IX_beatmap_identity");
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.Artist).HasColumnName("artist");
            entity.Property(k => k.ArtistUnicode).HasColumnName("artistU");
            entity.Property(k => k.Title).HasColumnName("title");
            entity.Property(k => k.TitleUnicode).HasColumnName("titleU");
            entity.Property(k => k.Creator).HasColumnName("creator");
            entity.Property(k => k.Version).HasColumnName("version");
            entity.Property(k => k.BeatmapFileName).HasColumnName("fileName");
            entity.Property(k => k.LastModifiedTime).HasColumnName("lastModified");
            entity.Property(k => k.DiffSrNoneStandard).HasColumnName("diffSrStd");
            entity.Property(k => k.DiffSrNoneTaiko).HasColumnName("diffSrTaiko");
            entity.Property(k => k.DiffSrNoneCtB).HasColumnName("diffSrCtb");
            entity.Property(k => k.DiffSrNoneMania).HasColumnName("diffSrMania");
            entity.Property(k => k.DrainTimeSeconds).HasColumnName("drainTime");
            entity.Property(k => k.TotalTime).HasColumnName("totalTime");
            entity.Property(k => k.AudioPreviewTime).HasColumnName("audioPreview");
            entity.Property(k => k.BeatmapId).HasColumnName("beatmapId");
            entity.Property(k => k.BeatmapSetId).HasColumnName("beatmapSetId");
            entity.Property(k => k.GameMode).HasColumnName("gameMode");
            entity.Property(k => k.SongSource).HasColumnName("source");
            entity.Property(k => k.SongTags).HasColumnName("tags");
            entity.Property(k => k.FolderName).HasColumnName("folderName");
            entity.Property(k => k.AudioFileName).HasColumnName("audioName");
            entity.Property(k => k.InOwnDb).HasColumnName("own");

            entity.Ignore(k => k.StarRatingStd);
            entity.Ignore(k => k.StarRatingTaiko);
            entity.Ignore(k => k.StarRatingCtb);
            entity.Ignore(k => k.StarRatingMania);
            entity.Ignore(k => k.AutoTitle);
            entity.Ignore(k => k.AutoArtist);
        }

        private static void ConfigureBeatmapSettings(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<BeatmapSettings>();

            entity.ToTable("map_info");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.Version).HasColumnName("version").IsRequired();
            entity.Property(k => k.FolderName).HasColumnName("folder").IsRequired();
            entity.Property(k => k.InOwnDb).HasColumnName("ownDb");
            entity.Property(k => k.Offset).HasColumnName("offset");
            entity.Property(k => k.LastPlayTime).HasColumnName("lastPlayTime");
            entity.Property(k => k.ExportFile).HasColumnName("exportFile");

            entity.Ignore(k => k.AddTime);
        }

        private static void ConfigureCollection(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Collection>();

            entity.ToTable("collection");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(k => k.Locked).HasColumnName("locked");
            entity.Property(k => k.Index).HasColumnName("index");
            entity.Property(k => k.ImagePath).HasColumnName("imagePath").HasMaxLength(700);
            entity.Property(k => k.Description).HasColumnName("description").HasMaxLength(700);
            entity.Property(k => k.CreateTime).HasColumnName("createTime");

            entity.Ignore(k => k.LockedBool);
        }

        private static void ConfigureCollectionRelation(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<CollectionRelation>();

            entity.ToTable("collection_relation");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.CollectionId).HasColumnName("collectionId").IsRequired();
            entity.Property(k => k.MapId).HasColumnName("mapId").IsRequired();
            entity.Property(k => k.AddTime).HasColumnName("addTime");
        }

        private static void ConfigureMapThumb(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<MapThumb>();

            entity.ToTable("map_thumb");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.MapId).HasColumnName("mapId");
            entity.Property(k => k.ThumbPath).HasColumnName("thumbPath");
        }

        private static void ConfigureStoryboardInfo(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<StoryboardInfo>();

            entity.ToTable("sb_info");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.MapId).HasColumnName("mapId").IsRequired();
            entity.Property(k => k.SbThumbPath).HasColumnName("thumbPath").IsRequired();
            entity.Property(k => k.SbThumbVideoPath).HasColumnName("thumbVideoPath").IsRequired();
            entity.Property(k => k.Version).HasColumnName("version").IsRequired();
            entity.Property(k => k.FolderName).HasColumnName("folder").IsRequired();
            entity.Property(k => k.InOwnDb).HasColumnName("own");
        }
    }
}
