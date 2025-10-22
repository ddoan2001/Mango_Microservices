using Mango.Cache.Interface;
using Mango.Cache.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace Mango.Cache.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds caching services to the dependency injection container using keyed services
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMangoCache(this IServiceCollection services)
        {
            // Register cache factory as singleton
            services.AddSingleton<ICacheFactory, CacheFactory>();

            // Option 1: Register with AddTransient
            //services.AddTransient<MemoryCacheManager>();
            //services.AddTransient<RedisCacheManager>();

            // Option2: Register cache managers with keyed services (Recommended)
            services.AddKeyedTransient<ICacheManager, MemoryCacheManager>(CacheResolverKeys.Memory);
            services.AddKeyedTransient<ICacheManager, RedisCacheManager>(CacheResolverKeys.Redis);

            // Note: To add a new cache type, simply add another line like:
            // services.AddKeyedTransient<ICacheManager, NewCacheManager>(CacheResolverKeys.NewType);

            return services;
        }
    }
}