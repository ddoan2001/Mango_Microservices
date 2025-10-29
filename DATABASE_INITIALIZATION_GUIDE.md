# Database Initialization Approaches - Complete Guide

## 🎯 **Three Flexible Approaches for Database Initialization**

The shared extensions now support multiple approaches for database initialization, giving you maximum flexibility based on your service needs.

---

## 🔥 **Approach 1: Interface-Based Override (Recommended for Complex Services)**

### ✅ **Best for**: Services with complex initialization logic (like AuthAPI with Identity seeding)

### **How it works:**
1. **Implement** `ICustomDatabaseInitializer` interface
2. **Register** the service in DI container
3. **Use** standard `InitializeDatabaseAsync()` - automatic detection

### **Implementation Example:**

```csharp
// 1. Create custom initializer
public class AuthDatabaseInitializer : ICustomDatabaseInitializer
{
    public async Task InitializeAsync(DbContext context, IServiceProvider serviceProvider)
    {
        if (context is not AppDbContext authContext)
            throw new ArgumentException($"Expected AppDbContext, but got {context.GetType().Name}");

        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        await SeedRolesDataAsync(authContext);
        await SeedUsersDataAsync(authContext, userManager);
        await authContext.SaveChangesAsync();
    }
    
    // ... seeding methods
}

// 2. Register in Program.cs
builder.Services.AddScoped<ICustomDatabaseInitializer, AuthDatabaseInitializer>();

// 3. Use standard initialization - automatic detection!
await app.InitializeDatabaseAsync<PostgreSqlAppDbContext, SqlServerAppDbContext>();
```

### **✅ Benefits:**
- **Automatic detection** - no extra parameters needed
- **Clean separation** - initialization logic in dedicated class
- **Dependency injection** - full access to all services
- **Type safety** - can cast to specific DbContext types
- **Reusable** - can be used across different contexts

---

## 🚀 **Approach 2: Function Parameter (Simple Custom Logic)**

### ✅ **Best for**: Services with simple, one-off initialization logic

### **How it works:**
1. **Create** a function that takes the DbContext
2. **Pass** it as parameter to `InitDbAsync()`
3. **The function** gets called after migrations

### **Implementation Example:**

```csharp
// 1. Create initialization function
var customInitializer = async (AppDbContext context) =>
{
    // Simple seeding logic
    if (!context.SomeTable.Any())
    {
        context.SomeTable.AddRange(
            new SomeEntity { Name = "Default 1" },
            new SomeEntity { Name = "Default 2" }
        );
        await context.SaveChangesAsync();
    }
};

// 2. Use with function parameter
var provider = app.Configuration["DatabaseProvider"];
if (provider == "PostgreSQL")
{
    await app.InitDbAsync<PostgreSqlAppDbContext>(customInitializer);
}
else
{
    await app.InitDbAsync<SqlServerAppDbContext>(customInitializer);
}
```

### **✅ Benefits:**
- **Simple and direct** - inline logic
- **No extra classes** - minimal code
- **Quick setup** - ideal for simple seeding

---

## ⚡ **Approach 3: Standard Migration Only (Default Behavior)**

### ✅ **Best for**: Services that only need database migrations

### **How it works:**
1. **Just call** `InitializeDatabaseAsync()` or `InitDbAsync()`
2. **Migrations run automatically**
3. **No custom logic** - clean and simple

### **Implementation Example:**

```csharp
// CouponAPI example - no custom initialization needed
await app.InitializeDatabaseAsync<PostgreSqlAppDbContext, SqlServerAppDbContext>();

// OR for single context
await app.InitDbAsync<AppDbContext>();
```

### **✅ Benefits:**
- **Minimal code** - one line initialization
- **Fast and clean** - just migrations
- **No overhead** - perfect for simple services

---

## 📊 **Priority System**

The shared extension uses this priority order:

1. **🥇 Function Parameter** - If `customInitializer` function is provided
2. **🥈 Interface Implementation** - If `ICustomDatabaseInitializer` service is registered
3. **🥉 Standard Behavior** - Just run migrations (no custom logic)

---

## 🎯 **Which Approach to Choose?**

| Service Type | Recommended Approach | Example |
|-------------|---------------------|---------|
| **Complex Identity/Auth** | Interface-Based Override | AuthAPI with user/role seeding |
| **Simple Data Seeding** | Function Parameter | ProductAPI with default categories |
| **Migration Only** | Standard Behavior | Basic CRUD APIs |
| **Multiple Contexts** | Interface-Based Override | Services with complex relationships |

---

## 🔧 **Migration Path for Existing Services**

### **From Old Custom DbInitializer:**

```csharp
// OLD WAY ❌
await DbInitializer.InitDb(app);

// NEW WAY ✅ - Option 1: Interface
builder.Services.AddScoped<ICustomDatabaseInitializer, CustomDatabaseInitializer>();
await app.InitializeDatabaseAsync<PostgreSqlAppDbContext, SqlServerAppDbContext>();

// NEW WAY ✅ - Option 2: Function
var customInit = CustomDbInitializer.GetInitializer(scope.ServiceProvider);
await app.InitDbAsync<AppDbContext>(customInit);

// NEW WAY ✅ - Option 3: Standard
await app.InitializeDatabaseAsync<PostgreSqlAppDbContext, SqlServerAppDbContext>();
```

---

## 🚀 **Summary**

This flexible approach gives you:

- **✅ Multiple options** for different complexity levels
- **✅ Automatic detection** for clean code
- **✅ Backward compatibility** with function parameters
- **✅ Provider flexibility** (PostgreSQL/SQL Server)
- **✅ Type safety** with generic constraints
- **✅ Dependency injection** support
- **✅ Clean separation** of concerns

**Choose the approach that best fits your service's complexity and requirements!**