using log4net;
using log4net.Config;
using Mango.Common.Extensions;
using Mango.Message.RabbitMQ.Models;
using Mango.Services.RewardAPI.Data;
using Mango.Services.RewardAPI.Messaging.RabbitMQ;
using Mango.Services.RewardAPI.Service;
using Mango.Services.RewardAPI.Service.IService;
using Mango.Services.RewardAPI.Utility;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure log4net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

// Add database provider (PostgreSQL/SQL Server support)
builder.AddDatabaseProvider<PostgreSqlAppDbContext, SqlServerAppDbContext, AppDbContext>();

// Configure RabbitMQ connection options
builder.Services.Configure<RabbitMQConnectionOptions>(builder.Configuration.GetSection(RabbitMQConnectionOptions.SectionName));

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRewardService, RewardService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<BackendApiAuthenticationHttpClientHandler>();

builder.Services.AddHttpClient("Order", u => u.BaseAddress =
new Uri(builder.Configuration["ServiceUrls:OrderAPI"])).AddHttpMessageHandler<BackendApiAuthenticationHttpClientHandler>();
builder.Services.AddHostedService<RabbitMQRewardConsumer>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
await app.InitializeDatabaseAsync<PostgreSqlAppDbContext, SqlServerAppDbContext>();

// app.UseAzureServiceBusConsumer();

app.Run();
