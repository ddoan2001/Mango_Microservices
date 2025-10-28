using Mango.Common.Configuration;
using Mango.Common.Extensions.Interface;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mango.Common.Extensions
{
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// Initializes the database based on the configured database provider
        /// </summary>
        /// <typeparam name="TPostgreSqlContext">PostgreSQL DbContext type</typeparam>
        /// <typeparam name="TSqlServerContext">SQL Server DbContext type</typeparam>
        /// <param name="app">The WebApplication</param>
        /// <returns>Task</returns>
        public static async Task InitializeDatabaseAsync<TPostgreSqlContext, TSqlServerContext>(this WebApplication app)
            where TPostgreSqlContext : DbContext
            where TSqlServerContext : DbContext
        {
            var provider = app.Configuration["DatabaseProvider"];

            switch (provider)
            {
                case DatabaseProviders.PostgreSQL:
                    await app.InitDbAsync<TPostgreSqlContext>();
                    break;
                case DatabaseProviders.SqlServer:
                    await app.InitDbAsync<TSqlServerContext>();
                    break;
                default:
                    throw new Exception($"Unsupported database provider: {provider}. Supported providers: {DatabaseProviders.PostgreSQL}, {DatabaseProviders.SqlServer}");
            }
        }

        /// <summary>
        /// Generic database initialization method that supports both custom functions and interface-based overrides
        /// Priority: 1. customInitializer function, 2. ICustomDatabaseInitializer service, 3. no custom logic
        /// </summary>
        /// <typeparam name="TContext">DbContext type</typeparam>
        /// <param name="app">The WebApplication</param>
        /// <param name="customInitializer">Optional custom initialization logic function</param>
        /// <returns>Task</returns>
        public static async Task InitDbAsync<TContext>(this WebApplication app, Func<TContext, Task>? customInitializer = null)
            where TContext : DbContext
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>()
                ?? throw new InvalidOperationException($"Failed to retrieve DbContext of type {typeof(TContext).Name}");

            // Run pending migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync();
            }

            // Priority 1: Use customInitializer function if provided
            if (customInitializer != null)
            {
                await customInitializer(context);
                return;
            }

            // Priority 2: Check if service implements ICustomDatabaseInitializer
            var customInitializerService = scope.ServiceProvider.GetService<ICustomDatabaseInitializer>();
            if (customInitializerService != null)
            {
                await customInitializerService.InitializeAsync(context, scope.ServiceProvider);
                return;
            }

            // Priority 3: No custom initialization - just migrations (normal behavior)
        }

        /// <summary>
        /// Simplified database initialization for single context scenarios
        /// </summary>
        /// <typeparam name="TContext">DbContext type</typeparam>
        /// <param name="app">The WebApplication</param>
        /// <param name="customInitializer">Optional custom initialization logic</param>
        /// <returns>Task</returns>
        public static async Task InitializeSingleDatabaseAsync<TContext>(this WebApplication app, Func<TContext, Task>? customInitializer = null)
            where TContext : DbContext
        {
            await app.InitDbAsync<TContext>(customInitializer);
        }
    }
}