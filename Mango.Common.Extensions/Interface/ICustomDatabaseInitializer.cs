using Microsoft.EntityFrameworkCore;

namespace Mango.Common.Extensions.Interface
{
    /// <summary>
    /// Interface for services that need custom database initialization logic
    /// Implement this interface to override the default InitDbAsync behavior
    /// </summary>
    public interface ICustomDatabaseInitializer
    {
        /// <summary>
        /// Custom initialization logic that will be called after migrations
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="serviceProvider">The scoped service provider</param>
        /// <returns>Task</returns>
        Task InitializeAsync(DbContext context, IServiceProvider serviceProvider);
    }
}
