using Coosu.Database;
using Coosu.Database.DataTypes;

namespace OsuPlayer.Data;

public static class OsuDbReaderExtensions
{
    public static IEnumerable<BeatmapCachedInfo> EnumerateDbModels(this OsuDbReader reader)
    {
        BeatmapCachedInfo? beatmap = null;

        while (reader.Read())
        {
            if (reader.NodeType == NodeType.ObjectStart)
            {
                beatmap = new BeatmapCachedInfo();
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

    private static void FillProperty(OsuDbReader reader, int nodeId, BeatmapCachedInfo beatmapInfo)
    {
        if (nodeId == 9) beatmapInfo.Artist = reader.GetString();
        else if (nodeId == 10) beatmapInfo.ArtistUnicode = reader.GetString();
        else if (nodeId == 11) beatmapInfo.Title = reader.GetString();
        else if (nodeId == 12) beatmapInfo.TitleUnicode = reader.GetString();
        else if (nodeId == 13) beatmapInfo.Creator = reader.GetString();
        else if (nodeId == 14) beatmapInfo.Version = reader.GetString();
        else if (nodeId == 15) beatmapInfo.AudioFileName = reader.GetString();
        else if (nodeId == 17) beatmapInfo.BeatmapFileName = reader.GetString();
        else if (nodeId == 22) beatmapInfo.LastModified = reader.GetDateTime();
        else if (nodeId == 29) FillStarRating(beatmapInfo, reader, DbGameMode.Circle);
        else if (nodeId == 32) FillStarRating(beatmapInfo, reader, DbGameMode.Taiko);
        else if (nodeId == 35) FillStarRating(beatmapInfo, reader, DbGameMode.Catch);
        else if (nodeId == 38) FillStarRating(beatmapInfo, reader, DbGameMode.Mania);
        else if (nodeId == 40) beatmapInfo.DrainTime = TimeSpan.FromSeconds(reader.GetInt32());
        else if (nodeId == 41) beatmapInfo.TotalTime = TimeSpan.FromMilliseconds(reader.GetInt32());
        else if (nodeId == 42) beatmapInfo.AudioPreviewTime = TimeSpan.FromMilliseconds(reader.GetInt32());
        else if (nodeId == 46) beatmapInfo.BeatmapId = reader.GetInt32();
        else if (nodeId == 47) beatmapInfo.BeatmapSetId = reader.GetInt32();
        else if (nodeId == 55) beatmapInfo.GameMode = (DbGameMode)reader.GetByte();
        else if (nodeId == 56) beatmapInfo.Source = reader.GetString();
        else if (nodeId == 57) beatmapInfo.Tags = reader.GetString();
        else if (nodeId == 63) beatmapInfo.FolderName = reader.GetString();
    }

    private static void FillStarRating(BeatmapCachedInfo beatmapInfo, OsuDbReader osuDbReader, DbGameMode index)
    {
        while (osuDbReader.Read())
        {
            if (osuDbReader.NodeType == NodeType.ArrayEnd) break;
            var data = osuDbReader.GetIntDoublePair();
            var mods = (Mods)data.IntValue;
            if (mods != Mods.None) continue;

            if (index == DbGameMode.Circle) beatmapInfo.DefaultStarRatingStd = data.DoubleValue;
            else if (index == DbGameMode.Taiko) beatmapInfo.DefaultStarRatingTaiko = data.DoubleValue;
            else if (index == DbGameMode.Catch) beatmapInfo.DefaultStarRatingCtB = data.DoubleValue;
            else if (index == DbGameMode.Mania) beatmapInfo.DefaultStarRatingMania = data.DoubleValue;
        }
    }
}