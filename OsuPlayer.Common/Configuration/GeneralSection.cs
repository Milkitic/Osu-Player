using System.Drawing;
using Milky.OsuPlayer.Presentation.Interaction;
using Path = System.IO.Path;
using Point = System.Windows.Point;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class GeneralSection
    {
        public bool RunOnStartup { get; set; } = false;
        public string DbPath { get; set; }
        public string CustomSongsPath { get; set; } = Path.Combine(Domain.CurrentPath, "Songs");
        public bool? ExitWhenClosed { get; set; } = null;
        public bool FirstOpen { get; set; } = true;
        public bool IsNavigationCollapsed { get; set; }
        public Point? MiniLastPosition { get; set; }
        public Rectangle? MiniWorkingArea { get; set; }
    }

    public class InterfaceSection : VmBase
    {
        private bool _minimalMode;

        public bool MinimalMode
        {
            get => _minimalMode;
            set
            {
                _minimalMode = value;
                OnPropertyChanged();
            }
        }

        public string Locale { get; set; }
    }
}