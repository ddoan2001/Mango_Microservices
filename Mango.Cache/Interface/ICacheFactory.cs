namespace Mango.Cache.Interface
{
    public interface ICacheFactory
    {
        ICacheManager GetCacheManager(string managerName);
        ICacheManager GetCacheManager(string session, string managerName);
        void ClearAll();
        void ClearUserCache(string session);
    }
}
