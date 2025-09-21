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
            var data = _cache.GetString(key);

            if (data is null)
                return default(T);

            return JsonSerializer.Deserialize<T>(data);
        }

        public void SetData<T>(string key, T data)
        {
            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = Expiration
            };

            _cache.SetString(key, JsonSerializer.Serialize(data), options);
        }
    }
}
