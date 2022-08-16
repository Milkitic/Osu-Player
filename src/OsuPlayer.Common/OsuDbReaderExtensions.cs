using System.Collections.Generic;
using Coosu.Beatmap.Sections.GamePlay;
using Coosu.Database;
using Coosu.Database.DataTypes;
using Beatmap = Milki.OsuPlayer.Data.Models.Beatmap;

namespace Milki.OsuPlayer.Common;

public static class OsuDbReaderExtensions
{
    public static IEnumerable<Beatmap> EnumerateBeatmapCustom(this OsuDbReader reader)
    {
        Beatmap beatmap = null;

        while (reader.Read())
        {
            if (reader.NodeType == NodeType.ObjectStart)
            {
                beatmap = new Beatmap();
                continue;
            }

            if (reader.NodeType == NodeType.ObjectEnd && beatmap != null)
            {
                yield return beatmap;
                beatmap = null;
            }

            if (reader.NodeType == NodeType.ArrayEnd && reader.NodeId == 7) yield break;
            if (beatmap == null) continue;
            if (reader.NodeType is not (NodeType.ArrayStart or NodeType.KeyValue)) continue;
            FillProperty(reader, reader.NodeId, beatmap);
        }
    }

    private static void FillProperty(OsuDbReader reader, int nodeId, Beatmap playItemDetail)
    {
        if (nodeId == 9) playItemDetail.Artist = reader.GetString();
        else if (nodeId == 10) playItemDetail.ArtistUnicode = reader.GetString();
        else if (nodeId == 11) playItemDetail.Title = reader.GetString();
        else if (nodeId == 12) playItemDetail.TitleUnicode = reader.GetString();
        else if (nodeId == 13) playItemDetail.Creator = reader.GetString();
        else if (nodeId == 14) playItemDetail.Version = reader.GetString();
        else if (nodeId == 15) playItemDetail.AudioFileName = reader.GetString();
        else if (nodeId == 17) playItemDetail.BeatmapFileName = reader.GetString();
        else if (nodeId == 22) playItemDetail.LastModifiedTime = reader.GetDateTime();
        else if (nodeId == 29) FillStarRating(playItemDetail, reader, DbGameMode.Circle);
        else if (nodeId == 32) FillStarRating(playItemDetail, reader, DbGameMode.Taiko);
        else if (nodeId == 35) FillStarRating(playItemDetail, reader, DbGameMode.Catch);
        else if (nodeId == 38) FillStarRating(playItemDetail, reader, DbGameMode.Mania);
        else if (nodeId == 40) playItemDetail.DrainTimeSeconds = reader.GetInt32();
        else if (nodeId == 41) playItemDetail.TotalTime = reader.GetInt32();
        else if (nodeId == 42) playItemDetail.AudioPreviewTime = reader.GetInt32();
        else if (nodeId == 46) playItemDetail.BeatmapId = reader.GetInt32();
        else if (nodeId == 47) playItemDetail.BeatmapSetId = reader.GetInt32();
        else if (nodeId == 55) playItemDetail.GameMode = (GameMode)reader.GetByte();
        else if (nodeId == 56) playItemDetail.SongSource = reader.GetString();
        else if (nodeId == 57) playItemDetail.SongTags = reader.GetString();
        else if (nodeId == 63) playItemDetail.FolderNameOrPath = reader.GetString();
    }

    private static void FillStarRating(Beatmap playItemDetail, OsuDbReader osuDbReader, DbGameMode index)
    {
        while (osuDbReader.Read())
        {
            if (osuDbReader.NodeType == NodeType.ArrayEnd) break;
            var data = osuDbReader.GetIntDoublePair();
            var mods = (Mods)data.IntValue;
            if (mods != Mods.None) continue;

            if (index == DbGameMode.Circle) playItemDetail.DiffSrNoneStandard = (long)(data.DoubleValue * 1_000_000_000);
            else if (index == DbGameMode.Taiko) playItemDetail.DiffSrNoneTaiko = (long)(data.DoubleValue * 1_000_000_000);
            else if (index == DbGameMode.Catch) playItemDetail.DiffSrNoneCtB = (long)(data.DoubleValue * 1_000_000_000);
            else if (index == DbGameMode.Mania) playItemDetail.DiffSrNoneMania = (long)(data.DoubleValue * 1_000_000_000);
        }
    }
}