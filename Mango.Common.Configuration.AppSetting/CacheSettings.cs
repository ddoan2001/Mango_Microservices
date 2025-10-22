namespace Mango.Common.Configuration.AppSetting
{
    /// <summary>
    /// Configuration settings for caching
    /// </summary>
    public class CacheSettings
    {
        public const string SectionName = "Cache";

        /// <summary>
        /// Memory cache configuration
        /// </summary>
        public MemoryCacheSettings Memory { get; set; } = new();

        /// <summary>
        /// Redis cache configuration
        /// </summary>
        public RedisCacheSettings Redis { get; set; } = new();

        /// <summary>
        /// Whether to use Redis as the default cache provider
        /// </summary>
        public bool UseRedis { get; set; } = false;
    }

    /// <summary>
    /// Memory cache specific settings
    /// </summary>
    public class MemoryCacheSettings
    {
        /// <summary>
        /// Default expiration time in seconds
        /// </summary>
        public int Expiration { get; set; } = 300; // 5 minutes default
    }

    /// <summary>
    /// Redis cache specific settings
    /// </summary>
    public class RedisCacheSettings
    {
        /// <summary>
        /// Default expiration time in seconds
        /// </summary>
        public int Expiration { get; set; } = 3600; // 1 hour default

        /// <summary>
        /// Database number to use (0-15)
        /// </summary>
        public int Database { get; set; } = 0;
    }
}