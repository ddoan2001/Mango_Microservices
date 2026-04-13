using AutoMapper;
using log4net;
using log4net.Config;
using Mango.Cache.Extensions;
using Mango.Common.Configuration;
using Mango.Common.Extensions;
using Mango.Services.ProductAPI;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Service;
using Mango.Services.ProductAPI.Service.IService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure log4net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

//var logDir = Path.Combine(AppContext.BaseDirectory, "logger");
//if (!Directory.Exists(logDir))
//{
//    Directory.CreateDirectory(logDir);
//}

// Add database provider (PostgreSQL/SQL Server support)
builder.AddDatabaseProvider<PostgreSqlAppDbContext, SqlServerAppDbContext, AppDbContext>();

builder.Services.AddStackExchangeRedisCache(option =>
{
    option.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
});

// Configure AutoMapper
builder.Services.AddAutoMapper(config =>
{
    config.AddProfile<MappingConfig>();
});

builder.Services.AddControllers();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<IProductService, ProductService>();

// Register custom database initializer
builder.Services.AddScoped<Mango.Common.Extensions.Interface.ICustomDatabaseInitializer, ProductDatabaseInitializer>();

// Configure cache settings from appsettings.json
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection(CacheSettings.SectionName));

// Register cache services
builder.Services.AddMangoCache();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme, securityScheme: new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter the Bearer Authorization string as following: `Bearer Generated-JWT-Token`",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference= new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id=JwtBearerDefaults.AuthenticationScheme
                }
            }, new string[]{}
        }
    });
});

builder.AddAppAuthentication();

builder.Services.AddAuthorization();
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins("https://localhost:3000"));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();

await app.InitializeDatabaseAsync<PostgreSqlAppDbContext, SqlServerAppDbContext>();

app.Run();