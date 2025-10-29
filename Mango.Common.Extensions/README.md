# Mango.Common.Extensions

This library provides shared extension methods for ASP.NET Core applications in the Mango microservices solution.

## Features

### WebApplicationBuilderExtensions

#### AddAppAuthentication()
Configures JWT authentication using settings from `appsettings.json`:

```csharp
builder.AddAppAuthentication();
```

**Required Configuration:**
```json
{
  "ApiSettings": {
    "Secret": "your-jwt-secret-key",
    "Issuer": "your-issuer",
    "Audience": "your-audience"
  }
}
```

#### AddDatabaseProvider<TPostgreSqlContext, TSqlServerContext, TBaseContext>()
Configures database provider based on configuration:

```csharp
builder.AddDatabaseProvider<PostgreSqlAppDbContext, SqlServerAppDbContext, AppDbContext>();
```

**Required Configuration:**
```json
{
  "DatabaseProvider": "SqlServer", // or "PostgreSQL"
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string"
  }
}
```

### WebApplicationExtensions

#### InitializeDatabaseAsync<TPostgreSqlContext, TSqlServerContext>()
Initializes database based on configured provider:

```csharp
await app.InitializeDatabaseAsync<PostgreSqlAppDbContext, SqlServerAppDbContext>();
```

#### InitDbAsync<TContext>(customInitializer?)
Generic database initialization with optional custom logic:

```csharp
// Simple migration
await app.InitDbAsync<AppDbContext>();

// With custom initialization
await app.InitDbAsync<AppDbContext>(async context => {
    // Add seed data or custom logic
    if (!context.SomeTable.Any()) {
        // Seed data
        await context.SaveChangesAsync();
    }
});
```

#### InitializeSingleDatabaseAsync<TContext>(customInitializer?)
For single database context scenarios:

```csharp
await app.InitializeSingleDatabaseAsync<AppDbContext>();
```

## Usage Example

```csharp
using Mango.Common.Extensions;
using YourProject.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure database
builder.AddDatabaseProvider<PostgreSqlAppDbContext, SqlServerAppDbContext, AppDbContext>();

// Configure authentication
builder.AddAppAuthentication();

// Other services...
builder.Services.AddControllers();

var app = builder.Build();

// Configure pipeline...
app.UseAuthentication();
app.UseAuthorization();

// Initialize database
await app.InitializeDatabaseAsync<PostgreSqlAppDbContext, SqlServerAppDbContext>();

app.Run();
```

## Benefits

- **Consistency**: Same authentication and database setup across all microservices
- **Maintainability**: Changes to authentication or database logic are centralized
- **Flexibility**: Support for multiple database providers and custom initialization
- **Reusability**: Easy to use in any ASP.NET Core project
- **Type Safety**: Generic methods ensure proper DbContext types