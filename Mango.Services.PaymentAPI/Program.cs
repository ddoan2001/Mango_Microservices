using log4net;
using log4net.Config;
using Mango.Message.RabbitMQ.Models;
using Mango.Message.RabbitMQ.Sender;
using Mango.Message.RabbitMQ.Sender.Interface;
using Mango.Services.PaymentAPI.Extensions;
using Mango.Services.PaymentAPI.Service;
using Mango.Services.PaymentAPI.Service.IService;
using Mango.Services.PaymentAPI.Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure log4net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

// Add services to the container.

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<BackendApiAuthenticationHttpClientHandler>();

// Configure RabbitMQ connection options
builder.Services.Configure<RabbitMQConnectionOptions>(builder.Configuration.GetSection(RabbitMQConnectionOptions.SectionName));
builder.Services.AddScoped<IRabbitMQSender>(provider =>
{
    var rabbitMQOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMQConnectionOptions>>().Value;
    return new RabbitMQSender(rabbitMQOptions);
});

builder.Services.AddHttpClient("Order", u => u.BaseAddress =
new Uri(builder.Configuration["ServiceUrls:OrderAPI"])).AddHttpMessageHandler<BackendApiAuthenticationHttpClientHandler>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

app.MapControllers();

app.Run();
