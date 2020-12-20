using System;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.Interaction;
using OSharp.Beatmap.Sections.GamePlay;

namespace Milky.OsuPlayer.Data
{
    //public class BeatmapViewModel : VmBase
    //{
    //    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    //    private double _displayStars;

    //    public Beatmap Beatmap { get; set; }
    //    public BeatmapThumb Thumb { get; set; }
    //    public BeatmapStoryboard Storyboard { get; set; }
    //    public BeatmapConfig Config { get; set; }

    //    public double DisplayStars
    //    {
    //        get => _displayStars;
    //        set
    //        {
    //            if (value.Equals(_displayStars)) return;
    //            _displayStars = value;
    //            OnPropertyChanged();
    //        }
    //    }

    //    public void CalculateStar()
    //    {
    //        try
    //        {
    //            DisplayStars = Beatmap.GameMode switch
    //            {
    //                GameMode.Circle => Math.Round(Beatmap.DiffSrNoneStandard, 2),
    //                GameMode.Taiko => Math.Round(Beatmap.DiffSrNoneTaiko, 2),
    //                GameMode.Catch => Math.Round(Beatmap.DiffSrNoneCtB, 2),
    //                GameMode.Mania => Math.Round(Beatmap.DiffSrNoneMania, 2),
    //                _ => DisplayStars
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.Error(ex);
    //        }
    //    }
    //}
}