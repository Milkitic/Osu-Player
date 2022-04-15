using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace OsuPlayer.Data.Models;

[Table("SoftwareStates")]
public sealed class SoftwareState
{
    public bool ShowWelcome { get; set; }
    public bool ShowFullNavigation { get; set; }
    public bool ShowMinimalWindow { get; set; }
    public Point? MinimalWindowPosition { get; set; }
    public Rectangle? MinimalWindowWorkingArea { get; set; }
    public DateTimeOffset? LastUpdateCheck { get; set; }
    public DateTime LastSync { get; set; }
    public string? IgnoredVersion { get; set; }
}