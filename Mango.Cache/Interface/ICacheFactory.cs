using Mango.Cache.Resolvers;

namespace Mango.Cache.Interface
{
    public interface ICacheFactory
    {
        ICacheManager GetCacheManager(string managerName);
        ICacheManager GetCacheManager(string session, string managerName);
        ICacheManager GetCacheManager(string managerName, CacheResolverKeys cacheType);
        ICacheManager GetCacheManager(string session, string managerName, CacheResolverKeys cacheType);
        void ClearAll();
        void ClearUserCache(string session);
    }
}
