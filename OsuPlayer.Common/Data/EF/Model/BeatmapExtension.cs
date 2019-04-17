namespace Milky.OsuPlayer.Common.Data.EF.Model {
    public static class BeatmapExtension
    {
        public static OSharp.Beatmap.Sections.GamePlay.GameMode ParseHollyToOSharp(this osu.Shared.GameMode gameMode)
        {
            return (OSharp.Beatmap.Sections.GamePlay.GameMode)(int)gameMode;

            #region not sure
            //switch (gameMode)
            //{
            //    case osu.Shared.GameMode.Standard:
            //        return OSharp.Beatmap.Sections.GamePlay.GameMode.Circle;
            //    case osu.Shared.GameMode.Taiko:
            //        return OSharp.Beatmap.Sections.GamePlay.GameMode.Taiko;
            //    case osu.Shared.GameMode.CatchTheBeat:
            //        return OSharp.Beatmap.Sections.GamePlay.GameMode.Catch;
            //    case osu.Shared.GameMode.Mania:
            //        return OSharp.Beatmap.Sections.GamePlay.GameMode.Mania;
            //    default:
            //        throw new ArgumentOutOfRangeException(nameof(gameMode), gameMode, null);
            //}
            #endregion
        }
    }
}