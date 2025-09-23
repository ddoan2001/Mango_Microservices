using System.Runtime.Caching;

namespace Mango.Cache.Interface
{
    public interface ICacheManager : System.IDisposable
    {
        /// <summary>
        /// Add or update an object to cache which will be slided or removed in expiration
        /// </summary>
        /// <param name="key">cache key</param>
        /// <param name="value">cache object</param>
        void AddOrUpdate<T>(string key, T value);

        /// <summary>
        /// Add or update an object to cache which will be slided or removed in expiration
        /// </summary>
        /// <param name="key">cache key</param>
        /// <param name="value">cache object</param>
        /// <param name="expires">sliding expiration time</param>
        /// <param name="CacheEntryEvictedCallback">delegate callback action when a cache entry is evicted</param>
        void AddOrUpdate<T>(string key, T value, TimeSpan expires, CacheEntryRemovedCallback CacheEntryEvictedCallback);

        /// <summary>
        /// Add or update an object to cache which will be slided or removed in expiration
        /// </summary>
        /// <param name="key">cache key</param>
        /// <param name="value">cache object</param>
        /// <param name="CacheEntryEvictedCallback">delegate callback action when a cache entry is evicted</param>
        void AddOrUpdate<T>(string key, T value, CacheEntryRemovedCallback CacheEntryEvictedCallback);

        /// <summary>
        /// Touch the cache entry to refresh the expiration time.
        /// </summary>
        /// <param name="key">Cache key</param>
        bool TouchKey(string key);

        /// <summary>
        /// Add or update an object to cache permanently which is never slided or removed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void AddOrUpdatePermanently<T>(string key, T value);

        /// <summary>
        /// Remove cache object by key
        /// </summary>
        /// <param name="key">cache key</param>
        void RemoveData(string key);

        /// <summary>
        /// Retrieve cache data by key
        /// </summary>
        /// <param name="key">cache key</param>
        /// <returns></returns>
        T? GetData<T>(string key);

        /// <summary>
        /// Check existing cache by key
        /// </summary>
        /// <param name="key">cache key</param>
        /// <returns></returns>
        bool Contains(string key);

        /// <summary>
        /// Clear all cache entries
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Clear cache entries by a keyword, entries start with keyword will be remove.
        /// </summary>
        /// <param name="keyword">Keyword to clear</param>
        void ClearCacheByKeyword(string keyword);

        /// <summary>
        /// Get caches
        /// </summary>
        /// <returns></returns>
        IEnumerable<KeyValuePair<string, T>> GetCaches<T>();
    }
}
