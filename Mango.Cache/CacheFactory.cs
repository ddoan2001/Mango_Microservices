using log4net;
using Mango.Cache.Interface;
using Mango.Cache.Resolvers;
using Mango.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Mango.Cache
{
    public sealed class CacheFactory : ICacheFactory
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CacheFactory));

        private readonly ConcurrentDictionary<CacheKey, ICacheManager> CacheObjects = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly CacheSettings _cacheSettings;

        private const string GeneralSession = "GeneralSession";

        public CacheFactory(IServiceProvider serviceProvider, IOptions<CacheSettings> cacheSettings)
        {
            _serviceProvider = serviceProvider;
            _cacheSettings = cacheSettings.Value;
        }

        public void ClearAll()
        {
            _logger.Warn("Forcing to clean up all system cache entries maybe lead to cause of other potential issues of the system!");

            var cacheEntries = CacheObjects.Keys.ToList();
            foreach (var cache in cacheEntries)
            {
                bool isRemoved = CacheObjects.TryRemove(cache, out ICacheManager? cacheEntry);
                if (isRemoved && cacheEntry != null)
                {
                    cacheEntry.Dispose();
                }
            }
        }

        public void ClearUserCache(string session)
        {
            _logger.InfoFormat("Clean up user cache entries: [{0}]", session);

            var cacheEntries = CacheObjects.Keys.Where(t => t.Session == session).ToList();

            foreach (var cache in cacheEntries)
            {
                CacheObjects.TryRemove(cache, out ICacheManager? cacheEntry);
                if (cacheEntry != null)
                {
                    cacheEntry.Dispose();
                }
            }
        }

        public ICacheManager GetCacheManager(string managerName)
        {
            // Use default cache type from configuration
            var defaultCacheType = GetDefaultCacheType();
            return GetCacheManager(GeneralSession, managerName, defaultCacheType);
        }

        public ICacheManager GetCacheManager(string session, string managerName)
        {
            // Use default cache type from configuration
            var defaultCacheType = GetDefaultCacheType();
            return GetCacheManager(session, managerName, defaultCacheType);
        }

        public ICacheManager GetCacheManager(string managerName, CacheResolverKeys cacheType)
        {
            return GetCacheManager(GeneralSession, managerName, cacheType);
        }

        public ICacheManager GetCacheManager(string session, string managerName, CacheResolverKeys cacheType)
        {
            var cache = new CacheKey
            {
                Name = $"{managerName}_{cacheType}", // Include cache type in the key to distinguish different cache types
                Session = session
            };

            if (string.IsNullOrEmpty(session)) cache.Session = GeneralSession;
            if (CacheObjects.ContainsKey(cache)) return CacheObjects[cache];

            // Create a new cache manager instance for this key with specified cache type
            var cacheManager = CreateCacheManager(cacheType);

            if (cacheManager == null)
            {
                throw new InvalidOperationException($"Failed to create cache manager instance for type '{cacheType}'.");
            }

            return CacheObjects.GetOrAdd(cache, cacheManager);
        }

        // Option 1: Implement with GetRequiredService
        //private ICacheManager CreateCacheManager(CacheResolverKeys cacheType)
        //{
        //    ICacheManager cacheManager = cacheType switch
        //    {
        //        CacheResolverKeys.Memory => _serviceProvider.GetRequiredService<MemoryCacheManager>(),
        //        CacheResolverKeys.Redis => _serviceProvider.GetRequiredService<RedisCacheManager>(),
        //        _ => throw new ArgumentException($"Unsupported cache type: {cacheType}")
        //    };

        //    return cacheManager;
        //}

        // Option 2: Implement with GetKeyedService (Recommended)
        private ICacheManager CreateCacheManager(CacheResolverKeys cacheType)
        {
            var cacheManager = _serviceProvider.GetKeyedService<ICacheManager>(cacheType);

            if (cacheManager == null)
            {
                throw new InvalidOperationException($"Cache manager for key '{cacheType}' not found. Make sure it's registered in DI container.");
            }

            return cacheManager;
        }

        private CacheResolverKeys GetDefaultCacheType()
        {
            // Check configuration to determine default cache type
            // Default to Memory if not configured or if Redis is not available
            if (_cacheSettings.UseRedis) return CacheResolverKeys.Redis;
            return CacheResolverKeys.Memory;
        }
    }
}
