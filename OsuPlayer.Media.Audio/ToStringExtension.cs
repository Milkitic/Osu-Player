using System;
using OSharp.Beatmap.Sections.HitObject;
using OSharp.Beatmap.Sections.Timing;

namespace Milky.OsuPlayer.Media.Audio
{
    public static class ToStringExtension
    {
        public static string ToHitsoundString(this TimingSamplesetType type)
        {
            switch (type)
            {
                case TimingSamplesetType.Soft:
                    return "soft";
                case TimingSamplesetType.Drum:
                    return "drum";
                default:
                case TimingSamplesetType.None:
                case TimingSamplesetType.Normal:
                    return "normal";
            }
        }

        public static string ToHitsoundString(this ObjectSamplesetType type, string sample)
        {
            switch (type)
            {
                case ObjectSamplesetType.Soft:
                    return "soft";
                case ObjectSamplesetType.Drum:
                    return "drum";
                case ObjectSamplesetType.Normal:
                    return "normal";
                case ObjectSamplesetType.Auto:
                    return sample;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}