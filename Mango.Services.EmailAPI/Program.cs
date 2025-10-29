using log4net;
using log4net.Config;
using Mango.Message.RabbitMQ.Models;
using Mango.Services.EmailAPI.Data;
using Mango.Services.EmailAPI.Messaging.RabbitMQ;
using Mango.Services.EmailAPI.Service;
using Mango.Services.EmailAPI.Service.IService;
using Mango.Message.RabbitMQ.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Reflection;
using Mango.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure log4net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

// Add database provider using shared extensions
builder.AddDatabaseProvider<PostgreSqlAppDbContext, SqlServerAppDbContext, AppDbContext>();

// Configure RabbitMQ connection options
builder.Services.Configure<RabbitMQConnectionOptions>(builder.Configuration.GetSection(RabbitMQConnectionOptions.SectionName));

builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddHostedService<RabbitMQAuthConsumer>();
builder.Services.AddHostedService<RabbitMQCartConsumer>();

// Add Health Checks
builder.Services.AddHealthChecks();

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

// Add Health Check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

// Initialize database using shared extensions
await app.InitializeDatabaseAsync<PostgreSqlAppDbContext, SqlServerAppDbContext>();

//app.UseAzureServiceBusConsumer();
app.Run();
