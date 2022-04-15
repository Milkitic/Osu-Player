namespace OsuPlayer.Shared.Configuration;

public sealed class SectionData
{
    public TimeSpan ScanOsuDbInterval { get; set; } = TimeSpan.FromDays(1);
    public string? OsuBaseFolder { get; set; }
    public string ExportMusicFolder { get; set; } = "./export/musics";
    public string ExportImageFolder { get; set; } = "./export/images";
    public ExportNaming ExportNaming { get; set; } = ExportNaming.ArtistTitle;
    public ExportGroup ExportGroup { get; set; } = ExportGroup.Artist;
}