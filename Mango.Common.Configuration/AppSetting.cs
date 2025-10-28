using Microsoft.Extensions.Configuration;


namespace Mango.Common.Configuration
{
    public class AppSetting
    {
        private readonly IConfiguration _configuration;

        public AppSetting(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //public string CacheMemoryExpiration => _configuration["Cache:Memory:Expiration"]
        //    ?? throw new Exception("Error when resolve config");
    }
}
