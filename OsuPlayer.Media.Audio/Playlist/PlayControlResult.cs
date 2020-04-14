namespace Milky.OsuPlayer.Media.Audio.Playlist
{
    public struct PlayControlResult
    {
        public enum PlayControlStatus
        {
            /// <summary>
            /// 强制播放
            /// </summary>
            Play,

            /// <summary>
            /// 强制停止
            /// </summary>
            Stop,

            /// <summary>
            /// 保持现有（改变列表时）
            /// </summary>
            Keep,

            /// <summary>
            /// 根据实际情况进行后续控制
            /// </summary>
            Unknown
        }

        public enum PointerControlStatus
        {
            /// <summary>
            /// 正常切换
            /// </summary>
            Default,

            /// <summary>
            /// 强制置0
            /// </summary>
            Reset,

            /// <summary>
            /// 强制保持
            /// </summary>
            Keep,

            /// <summary>
            /// 强制清空
            /// </summary>
            Clear
        }

        public PlayControlResult(PlayControlStatus playStatus, PointerControlStatus pointerStatus)
        {
            PlayStatus = playStatus;
            PointerStatus = pointerStatus;
        }

        public PlayControlStatus PlayStatus { get; set; }
        public PointerControlStatus PointerStatus { get; set; }

        public override string ToString()
        {
            return $"{PointerStatus}+{PlayStatus}";
        }
    }
}