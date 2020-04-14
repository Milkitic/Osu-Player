namespace Milky.OsuPlayer.Data.Dapper
{
    public class DbPoolConfiguration
    {
        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxPoolSize { get; set; } = 10;

        /// <summary>
        /// 最小连接数
        /// </summary>
        public int MinPoolSize { get; set; } = 5;

        /// <summary>
        /// 是否异步访问数据库
        /// </summary>
        public bool AsynchronousProcessing { get; set; } = true;

        /// <summary>
        /// 连接等待时间
        /// </summary>
        public int ConnectionTimeout { get; set; } = 15;

        /// <summary>
        /// 连接的生命周期
        /// </summary>
        public int ConnectionLifetime { get; set; } = 15;

        public bool IsDefault { get; private set; }
        public static DbPoolConfiguration Default => new DbPoolConfiguration {IsDefault = true};
    }
}