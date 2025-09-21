using log4net;
using Mango.Cache.Interface;
using Mango.Common.Configuration.AppSetting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Mango.Cache
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly CacheSettings _cacheSettings;
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private bool _isRedisAvailable = true;

        public RedisCacheService(IOptions<CacheSettings> cacheOptions, IDistributedCache cache)
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
                return default(T);
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

        public void SetData<T>(string key, T data)
        {
            if (!_isRedisAvailable)
            {
                return;
            }

            try
            {
                var options = new DistributedCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = Expiration
                };

                _cache.SetString(key, JsonSerializer.Serialize(data), options);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to set data to Redis cache for key: {key}", ex);
                _isRedisAvailable = false;

                // Schedule a retry after some time
                _ = Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => _isRedisAvailable = true);
            }
        }
    }
}
