using log4net;
using Mango.Cache.Interface;
using Mango.Common.Configuration.AppSetting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Runtime.Caching;
using System.Text.Json;

namespace Mango.Cache
{
    public class RedisCacheManager : ICacheManager
    {
        private readonly IDistributedCache _cache;
        private readonly CacheSettings _cacheSettings;
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private bool _isRedisAvailable = true;

        public RedisCacheManager(IOptions<CacheSettings> cacheOptions, IDistributedCache cache)
        {
            _cacheSettings = cacheOptions.Value;
            _cache = cache;
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

        public T? GetData<T>(string key)
        {
            if (!_isRedisAvailable)
            {
                return default;
            }

            try
            {
                var data = _cache.GetString(key);

                if (data is null)
                    return default(T);

                return JsonSerializer.Deserialize<T>(data);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get data from Redis cache for key: {key}", ex);
                _isRedisAvailable = false;

                // Schedule a retry after some time
                _ = Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => _isRedisAvailable = true);

                return default(T);
            }
        }

        public void AddOrUpdate<T>(string key, T value)
        {
            AddOrUpdate(key, value, Expiration, null!);
        }

        public void AddOrUpdate<T>(string key, T value, TimeSpan expires, CacheEntryRemovedCallback CacheEntryEvictedCallback)
        {
            if (!_isRedisAvailable)
            {
                return;
            }

            try
            {
                var options = new DistributedCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = expires
                };

                _cache.SetString(key, JsonSerializer.Serialize(value), options);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to set data to Redis cache for key: {key}", ex);
                _isRedisAvailable = false;

                // Schedule a retry after some time
                _ = Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => _isRedisAvailable = true);
            }
        }

        public void AddOrUpdate<T>(string key, T value, CacheEntryRemovedCallback CacheEntryEvictedCallback)
        {
            throw new NotImplementedException();
        }

        public bool TouchKey(string key)
        {
            var obj = _cache.Get(key);
            obj = null;
            return true;
        }

        public void AddOrUpdatePermanently<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public void RemoveData(string key)
        {
            throw new NotImplementedException();
        }

        public bool Contains(string key)
        {
            try
            {
                var data = _cache.Get(key);
                return data != null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to check if key exists in Redis cache: {key}", ex);
                return false;
            }
        }

        public void ClearCache()
        {
            throw new NotImplementedException();
        }

        public void ClearCacheByKeyword(string keyword)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValuePair<string, T>> GetCaches<T>()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
