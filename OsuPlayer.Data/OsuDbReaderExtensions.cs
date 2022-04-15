using Coosu.Database;
using Coosu.Database.DataTypes;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Data;

public static class OsuDbReaderExtensions
{
    public static IEnumerable<PlayItemDetail> EnumerateDbModels(this OsuDbReader reader)
    {
        PlayItemDetail? beatmap = null;

        while (reader.Read())
        {
            if (reader.NodeType == NodeType.ObjectStart)
            {
                beatmap = new PlayItemDetail();
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

    private static void FillProperty(OsuDbReader reader, int nodeId, PlayItemDetail playItemDetail)
    {
        if (nodeId == 9) playItemDetail.Artist = reader.GetString();
        else if (nodeId == 10) playItemDetail.ArtistUnicode = reader.GetString();
        else if (nodeId == 11) playItemDetail.Title = reader.GetString();
        else if (nodeId == 12) playItemDetail.TitleUnicode = reader.GetString();
        else if (nodeId == 13) playItemDetail.Creator = reader.GetString();
        else if (nodeId == 14) playItemDetail.Version = reader.GetString();
        else if (nodeId == 15) playItemDetail.AudioFileName = reader.GetString();
        else if (nodeId == 17) playItemDetail.BeatmapFileName = reader.GetString();
        else if (nodeId == 22) playItemDetail.LastModified = reader.GetDateTime();
        else if (nodeId == 29) FillStarRating(playItemDetail, reader, DbGameMode.Circle);
        else if (nodeId == 32) FillStarRating(playItemDetail, reader, DbGameMode.Taiko);
        else if (nodeId == 35) FillStarRating(playItemDetail, reader, DbGameMode.Catch);
        else if (nodeId == 38) FillStarRating(playItemDetail, reader, DbGameMode.Mania);
        else if (nodeId == 40) playItemDetail.DrainTime = TimeSpan.FromSeconds(reader.GetInt32());
        else if (nodeId == 41) playItemDetail.TotalTime = TimeSpan.FromMilliseconds(reader.GetInt32());
        else if (nodeId == 42) playItemDetail.AudioPreviewTime = TimeSpan.FromMilliseconds(reader.GetInt32());
        else if (nodeId == 46) playItemDetail.BeatmapId = reader.GetInt32();
        else if (nodeId == 47) playItemDetail.BeatmapSetId = reader.GetInt32();
        else if (nodeId == 55) playItemDetail.GameMode = (DbGameMode)reader.GetByte();
        else if (nodeId == 56) playItemDetail.Source = reader.GetString();
        else if (nodeId == 57) playItemDetail.Tags = reader.GetString();
        else if (nodeId == 63) playItemDetail.FolderName = reader.GetString();
    }

    private static void FillStarRating(PlayItemDetail playItemDetail, OsuDbReader osuDbReader, DbGameMode index)
    {
        while (osuDbReader.Read())
        {
            if (osuDbReader.NodeType == NodeType.ArrayEnd) break;
            var data = osuDbReader.GetIntDoublePair();
            var mods = (Mods)data.IntValue;
            if (mods != Mods.None) continue;

            if (index == DbGameMode.Circle) playItemDetail.DefaultStarRatingStd = (long)(data.DoubleValue * 10);
            else if (index == DbGameMode.Taiko) playItemDetail.DefaultStarRatingTaiko = (long)(data.DoubleValue * 10);
            else if (index == DbGameMode.Catch) playItemDetail.DefaultStarRatingCtB = (long)(data.DoubleValue * 10);
            else if (index == DbGameMode.Mania) playItemDetail.DefaultStarRatingMania = (long)(data.DoubleValue * 10);
        }
    }
}