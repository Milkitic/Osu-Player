using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace Milki.OsuPlayer.Data.Models;

[Table("SoftwareStates")]
public sealed class SoftwareState
{
    public int Id { get; set; }
    public bool ShowWelcome { get; set; }
    public bool ShowFullNavigation { get; set; }
    public bool ShowMinimalWindow { get; set; }
    [MaxLength(32)]
    public Point? MinimalWindowPosition { get; set; }
    [MaxLength(32)]
    public Rectangle? MinimalWindowWorkingArea { get; set; }
    public DateTime? LastUpdateCheck { get; set; }
    public DateTime? LastSync { get; set; }
    [MaxLength(32)]
    public string? IgnoredVersion { get; set; }
}