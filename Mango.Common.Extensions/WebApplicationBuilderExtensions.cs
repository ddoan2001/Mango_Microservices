using Mango.Common.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Mango.Common.Extensions
{
    public static class WebApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds JWT authentication to the application
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder</param>
        /// <returns>The WebApplicationBuilder for chaining</returns>
        public static WebApplicationBuilder AddAppAuthentication(this WebApplicationBuilder builder)
        {
            var settingsSection = builder.Configuration.GetSection("ApiSettings");

            var secret = settingsSection.GetValue<string>("Secret");
            var issuer = settingsSection.GetValue<string>("Issuer");
            var audience = settingsSection.GetValue<string>("Audience");

            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("JWT Secret is required in ApiSettings:Secret");

            var key = Encoding.ASCII.GetBytes(secret);

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    ValidateAudience = true
                };
            });

            return builder;
        }

        /// <summary>
        /// Adds database provider configuration based on DatabaseProvider setting
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder</param>
        /// <param name="configurePostgreSQL">Optional action to configure PostgreSQL context</param>
        /// <param name="configureSqlServer">Optional action to configure SQL Server context</param>
        /// <returns>The WebApplicationBuilder for chaining</returns>
        public static WebApplicationBuilder AddDatabaseProvider<TPostgreSqlContext, TSqlServerContext, TBaseContext>(
            this WebApplicationBuilder builder,
            Action<DbContextOptionsBuilder>? configurePostgreSQL = null,
            Action<DbContextOptionsBuilder>? configureSqlServer = null)
            where TPostgreSqlContext : DbContext, TBaseContext
            where TSqlServerContext : DbContext, TBaseContext
            where TBaseContext : class
        {
            var provider = builder.Configuration["DatabaseProvider"];
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("DefaultConnection string is required");

            switch (provider)
            {
                case DatabaseProviders.PostgreSQL:
                    builder.Services.AddDbContext<TPostgreSqlContext>(options =>
                    {
                        options.UseNpgsql(connectionString);
                        configurePostgreSQL?.Invoke(options);
                    });
                    builder.Services.AddScoped<TBaseContext>(provider =>
                        provider.GetRequiredService<TPostgreSqlContext>());
                    break;

                case DatabaseProviders.SqlServer:
                    builder.Services.AddDbContext<TSqlServerContext>(options =>
                    {
                        options.UseSqlServer(connectionString);
                        configureSqlServer?.Invoke(options);
                    });
                    builder.Services.AddScoped<TBaseContext>(provider =>
                        provider.GetRequiredService<TSqlServerContext>());
                    break;

                default:
                    throw new Exception($"Unsupported database provider: {provider}. Supported providers: {DatabaseProviders.PostgreSQL}, {DatabaseProviders.SqlServer}");
            }

            return builder;
        }
    }
}