using log4net;
using Mango.Cache.Interface;
using System.Collections.Concurrent;

namespace Mango.Cache
{
    public sealed class CacheFactory(ICacheManager cacheManager) : ICacheFactory
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ConcurrentDictionary<CacheKey, ICacheManager> CacheObjects = new ConcurrentDictionary<CacheKey, ICacheManager>();

        private const string GeneralSession = "GeneralSession";

        public void ClearAll()
        {
            _logger.Warn("Forcing to clean up all system cache entries maybe lead to cause of other potential issues of the system!");

            var cacheEntries = CacheObjects.Keys.ToList();
            foreach (var cache in cacheEntries)
            {
                bool isRemoved = CacheObjects.TryRemove(cache, out ICacheManager cacheEntry);
                if (isRemoved)
                {
                    cacheEntry?.Dispose();
                    cacheEntry = null;
                }
            }
        }

        public void ClearUserCache(string session)
        {
            _logger.InfoFormat("Clean up user cache entries: [{0}]", session);

            var cacheEntries = CacheObjects.Keys.Where(t => t.Session == session).ToList();

            foreach (var cache in cacheEntries)
            {
                CacheObjects.TryRemove(cache, out ICacheManager cacheEntry);
                cacheEntry?.Dispose();
                cacheEntry = null;
            }
        }

        public ICacheManager GetCacheManager(string managerName)
        {
            return GetCacheManager(GeneralSession, managerName);
        }

        public ICacheManager GetCacheManager(string session, string managerName)
        {
            var cache = new CacheKey
            {
                Name = managerName,
                Session = session
            };

            if (string.IsNullOrEmpty(session)) cache.Session = GeneralSession;
            if (CacheObjects.ContainsKey(cache)) return CacheObjects[cache];

            return CacheObjects.GetOrAdd(cache, cacheManager);
        }
    }
}
