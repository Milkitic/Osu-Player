using System;
using System.Collections.Generic;
using Coosu.Beatmap.Sections.GamePlay;
using Coosu.Database;
using Coosu.Database.DataTypes;
using Coosu.Database.Generated;
using Beatmap = Milky.OsuPlayer.Data.Models.Beatmap;

namespace Milky.OsuPlayer.Common
{
    public static class OsuDbReaderExtensions
    {
        public static IEnumerable<Beatmap> EnumerateBeatmapsCustom(this OsuDbReader reader)
        {
            Beatmap beatmap = null;
            while (!reader.IsEndOfStream && reader.Read())
            {
                if (reader.NodeType == NodeType.ObjectStart)
                {
                    beatmap = new Beatmap();
                    continue;
                }

                if (reader.NodeType == NodeType.ObjectEnd && beatmap != null)
                {
                    yield return beatmap;
                    beatmap = default;
                }

                if (reader.NodeType == NodeType.ArrayEnd && reader.NodeId == (int)NodeId.OsuDb_BeatmapArray)
                {
                    yield break;
                }

                if (beatmap == default)
                {
                    continue;
                }

                if (reader.NodeType is not (NodeType.ArrayStart or NodeType.KeyValue))
                {
                    continue;
                }

                FillProperty(reader, beatmap);
            }
        }

        private static void FillProperty(OsuDbReader reader, Beatmap beatmap)
        {
            var nodeId = (NodeId)reader.NodeId;
            if (nodeId == NodeId.Artist) beatmap.Artist = reader.GetString();
            else if (nodeId == NodeId.ArtistUnicode) beatmap.ArtistUnicode = reader.GetString();
            else if (nodeId == NodeId.Title) beatmap.Title = reader.GetString();
            else if (nodeId == NodeId.TitleUnicode) beatmap.TitleUnicode = reader.GetString();
            else if (nodeId == NodeId.Creator) beatmap.Creator = reader.GetString();
            else if (nodeId == NodeId.Difficulty) beatmap.Version = reader.GetString();
            else if (nodeId == NodeId.AudioFileName) beatmap.AudioFileName = reader.GetString();
            else if (nodeId == NodeId.FileName) beatmap.BeatmapFileName = reader.GetString();
            else if (nodeId == NodeId.LastModified) beatmap.LastModifiedTime = reader.GetDateTime();
            else if (nodeId == NodeId.StarRatingStdArray) FillStarRating(beatmap.StarRatingStd = new(), reader);
            else if (nodeId == NodeId.StarRatingTaikoArray) FillStarRating(beatmap.StarRatingTaiko = new(), reader);
            else if (nodeId == NodeId.StarRatingCtbArray) FillStarRating(beatmap.StarRatingCtb = new(), reader);
            else if (nodeId == NodeId.StarRatingManiaArray) FillStarRating(beatmap.StarRatingMania = new(), reader);
            else if (nodeId == NodeId.DrainTime) beatmap.DrainTimeSeconds = reader.GetInt32();
            else if (nodeId == NodeId.TotalTime) beatmap.TotalTime = reader.GetInt32();
            else if (nodeId == NodeId.AudioPreviewTime) beatmap.AudioPreviewTime = reader.GetInt32();
            else if (nodeId == NodeId.BeatmapId) beatmap.BeatmapId = reader.GetInt32();
            else if (nodeId == NodeId.BeatmapSetId) beatmap.BeatmapSetId = reader.GetInt32();
            else if (nodeId == NodeId.GameMode) beatmap.GameMode = (GameMode)reader.GetByte();
            else if (nodeId == NodeId.Source) beatmap.SongSource = reader.GetString();
            else if (nodeId == NodeId.Tags) beatmap.SongTags = reader.GetString();
            else if (nodeId == NodeId.FolderName) beatmap.FolderName = reader.GetString();
        }

        private static void FillStarRating(Dictionary<Mods, float> dictionary, OsuDbReader reader)
        {
            while (!reader.IsEndOfStream && reader.Read())
            {
                if (reader.NodeType == NodeType.ArrayEnd) break;
                var data = reader.GetIntSinglePair();
                var mods = (Mods)data.IntValue;
                dictionary.Add(mods, data.SingleValue);
            }
        }
    }
}