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
}