using Mango.Cache.Interface;
using Mango.Common.Configuration.AppSetting;
using Microsoft.Extensions.Options;
using System.Runtime.Caching;

namespace Mango.Cache
{
    public sealed class MemoryCacheManager : ICacheManager
    {
        private readonly MemoryCache _cache = new("CacheConfiguration");
        private readonly CacheSettings _cacheSettings;

        public MemoryCacheManager(IOptions<CacheSettings> cacheOptions)
        {
            _cacheSettings = cacheOptions.Value;
        }

        private TimeSpan Expiration
        {
            get
            {
                int expirationTime = _cacheSettings.Memory.Expiration;

                // Ensure minimum expiration time of 30 seconds
                if (expirationTime <= 0)
                    expirationTime = 300; // 5 minutes default

                return TimeSpan.FromSeconds(expirationTime);
            }
        }


        public void AddOrUpdate<T>(string key, T value)
        {
            AddOrUpdate(key, value, Expiration, null!);
        }

        public void AddOrUpdate<T>(string key, T value, TimeSpan expires, CacheEntryRemovedCallback CacheEntryEvictedCallback)
        {
            var policy = new CacheItemPolicy
            {
                SlidingExpiration = expires,
                RemovedCallback = CacheEntryEvictedCallback
            };
            if (value != null) _cache.Set(key, value, policy);
        }

        /// <summary>
        /// Add or update an object to cache which will be slided or removed in expiration
        /// </summary>
        /// <param name="key">cache key</param>
        /// <param name="value">cache object</param>
        /// <param name="CacheEntryEvictedCallback">delegate callback action when a cache entry is evicted</param>
        public void AddOrUpdate<T>(string key, T value, CacheEntryRemovedCallback CacheEntryEvictedCallback)
        {
            var policy = new CacheItemPolicy
            {
                SlidingExpiration = Expiration,
                RemovedCallback = CacheEntryEvictedCallback
            };
            if (value != null) _cache.Set(key, value, policy);
        }

        /// <summary>
        /// Touch the cache entry to refresh the expiration time.
        /// </summary>
        /// <param name="key">Cache key</param>
        public bool TouchKey(string key)
        {
            var obj = _cache.Get(key);
            obj = null;
            return true;
        }

        /// <summary>
        /// Cache objects for lifetime of application
        /// </summary>
        /// <param name="key">cache key</param>
        /// <param name="value">cache object</param>
        public void AddOrUpdatePermanently<T>(string key, T value)
        {
            var policy = new CacheItemPolicy
            {
                Priority = CacheItemPriority.NotRemovable
            };
            if (value != null) _cache.Set(key, value, policy);
        }

        /// <summary>
        /// Clean up all cache entries
        /// </summary>
        public void ClearCache()
        {
            _cache.Trim(100);
        }

        public T? GetData<T>(string key)
        {
            return (T?)_cache.Get(key);
        }

        public void RemoveData(string key)
        {
            if (_cache.Contains(key)) _cache.Remove(key);
        }

        public bool Contains(string key)
        {
            return _cache.Contains(key);
        }


        public void Dispose()
        {
            _cache.Trim(100);
            _cache.Dispose();
        }

        /// <summary>
        /// Clear cache entries by a keyword, entries start with keyword will be remove.
        /// </summary>
        /// <param name="keyword">Keyword to clear</param>
        public void ClearCacheByKeyword(string keyword)
        {
            foreach (var entry in _cache)
            {
                if (entry.Key.StartsWith(keyword))
                {
                    _cache.Remove(entry.Key);
                }
            }
        }

        /// <summary>
        /// Get caches
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, T>> GetCaches<T>()
        {
            return _cache.Where(cache => cache.Value is T).Select(cache => new KeyValuePair<string, T>(cache.Key, (T)cache.Value));
        }
    }
}
