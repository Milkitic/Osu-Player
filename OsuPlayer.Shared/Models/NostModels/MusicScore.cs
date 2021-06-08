using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Milky.OsuPlayer.Shared.Models.NostModels
{
    [Serializable]
    [XmlRoot("music_score")]
    public class MusicScore
    {
        [XmlElement("header")]
        public MusicScoreHeader Header { get; set; }
        [XmlArray("note_data")]
        [XmlArrayItem("note", typeof(MusicScoreNote))]
        public List<MusicScoreNote> NoteData { get; set; }
        [XmlArray("event_data")]
        [XmlArrayItem("event", typeof(MusicScoreEvent))]
        public List<MusicScoreEvent> EventData { get; set; }
        [XmlArray("beat_data")]
        [XmlArrayItem("beat", typeof(MusicScoreBeat))]
        public List<MusicScoreBeat> BeatData { get; set; }
        [XmlArray("track_info")]
        [XmlArrayItem("track", typeof(MusicScoreTrack))]
        public List<MusicScoreTrack> TrackInfo { get; set; }
    }
    [Serializable]
    public class MusicScoreHeader
    {
        [XmlElement("max_scale")]
        public int MaxScale { get; set; }
        [XmlElement("min_scale")]
        public int MinScale { get; set; }
        [XmlElement("file_version")]
        public short FileVersion { get; set; }
        [XmlElement("first_bpm")]
        public long FirstBpm { get; set; }
        [XmlElement("music_finish_time_msec")]
        public int MusicFinishTimeMsec { get; set; }
    }
    [Serializable]
    public class MusicScoreNote
    {
        [XmlElement("index")]
        public int Index { get; set; }
        [XmlElement("start_timing_msec")]
        public int StartTimingMsec { get; set; }
        [XmlElement("end_timing_msec")]
        public int EndTimingMsec { get; set; }
        [XmlElement("gate_time_msec")]
        public int GateTimeMsec { get; set; }
        [XmlElement("scale_piano")]
        public byte ScalePiano { get; set; }
        [XmlElement("min_key_index")]
        public int MinKeyIndex { get; set; }
        [XmlElement("max_key_index")]
        public int MaxKeyIndex { get; set; }
        [XmlElement("note_type")]
        public int NoteType { get; set; }
        [XmlElement("hand")]
        public int Hand { get; set; }
        [XmlElement("key_kind")]
        public int KeyKind { get; set; }
        [XmlElement("param1")]
        public int Param1 { get; set; }
        [XmlElement("param2")]
        public int Param2 { get; set; }
        [XmlElement("param3")]
        public int Param3 { get; set; }
        [XmlArray("sub_note_data")]
        [XmlArrayItem("sub_note", typeof(MusicScoreSubNote))]
        public List<MusicScoreSubNote> SubNoteData { get; set; }
    }
    [Serializable]
    public class MusicScoreSubNote
    {
        [XmlElement("start_timing_msec")]
        public int StartTimingMsec { get; set; }
        [XmlElement("end_timing_msec")]
        public int EndTimingMsec { get; set; }
        [XmlElement("scale_piano")]
        public byte ScalePiano { get; set; }
        [XmlElement("velocity")]
        public byte Velocity { get; set; }
        [XmlElement("track_index")]
        public int TrackIndex { get; set; }
        [XmlIgnore]
        public float Balance { get; set; }
    }
    [Serializable]
    public class MusicScoreEvent
    {
        [XmlElement("index")]
        public int Index { get; set; }
        [XmlElement("start_timing_msec")]
        public int StartTimingMsec { get; set; }
        [XmlElement("type")]
        public int Type { get; set; }
        [XmlElement("value")]
        public long Value { get; set; }
    }
    [Serializable]
    public class MusicScoreBeat
    {
        [XmlElement("index")]
        public int Index { get; set; }
        [XmlElement("start_timing_msec")]
        public int StartTimingMsec { get; set; }
    }
    [Serializable]
    public class MusicScoreTrack
    {
        [XmlElement("index")]
        public int Index { get; set; }
        [XmlElement("name")]
        public string Name { get; set; }
    }
}
