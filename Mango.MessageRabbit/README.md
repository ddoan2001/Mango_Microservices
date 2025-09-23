# Mango.Message.RabbitMQ

A comprehensive RabbitMQ messaging library for .NET 8 that provides unified consumer and sender implementations with support for both simple queue and exchange-based messaging patterns.

## 🚀 Features

- **Unified Consumer Base**: Single base class supporting multiple consumption patterns
- **Thread-Safe Sender**: Reliable message publishing with connection management
- **Professional Logging**: Integrated log4net support with structured logging
- **Production Ready**: Comprehensive error handling and resource management
- **Flexible Configuration**: Support for both simple queue and exchange-based patterns
- **Background Service Integration**: Built-in support for ASP.NET Core hosted services

## 📦 Package Information

- **Target Framework**: .NET 8.0
- **Nullable Reference Types**: Enabled
- **Dependencies**:
  - RabbitMQ.Client 7.1.2
  - log4net 3.1.0 + log4net.Ext.Json 3.0.3
  - Microsoft.Extensions.Hosting.Abstractions 8.0.0
  - Newtonsoft.Json 13.0.3

## 🏗️ Architecture

### Core Components

1. **RabbitMQBaseConsumer** - Abstract base class for message consumers
2. **RabbitMQSender** - Thread-safe message sender implementation
3. **IRabbitMQSender** - Interface for message publishing operations

### Project Structure

```
Mango.MessageRabbit/
├── Consumer/
│   └── Base/
│       └── RabbitMQBaseConsumer.cs          # Abstract consumer base class
├── Sender/
│   ├── RabbitMQSender.cs                    # Message sender implementation
│   └── ISender/
│       └── IRabbitMQSender.cs               # Sender interface
├── log4net.config                           # Logging configuration
└── Mango.Message.RabbitMQ.csproj           # Project file
```

## 🔧 Usage

### Consumer Implementation

The `RabbitMQBaseConsumer` supports two messaging patterns:

#### 1. Simple Queue Pattern (Direct Queue Consumption)

```csharp
using Mango.Message.RabbitMQ.Consumer.Base;

public class EmailConsumer : RabbitMQBaseConsumer
{
    // Override QueueName for simple queue consumption
    protected override string? QueueName => "email.queue";

    protected override async Task HandleMessageAsync(string body)
    {
        // Deserialize and process your message
        var emailMessage = JsonConvert.DeserializeObject<EmailMessage>(body);

        // Your business logic here
        await _emailService.SendEmailAsync(emailMessage);

        _logger.Info($"Processed email message: {emailMessage.Id}");
    }
}
```

#### 2. Exchange Pattern (Exchange + Routing Key)

```csharp
using Mango.Message.RabbitMQ.Consumer.Base;

public class OrderConsumer : RabbitMQBaseConsumer
{
    // Override ExchangeName and Queue for exchange-based consumption
    protected override string? ExchangeName => "order.exchange";

    protected override KeyValuePair<string, string> Queue => new(
        "order.created",        // Routing key
        "order.processing"      // Queue name
    );

    protected override async Task HandleMessageAsync(string body)
    {
        // Deserialize and process your message
        var orderMessage = JsonConvert.DeserializeObject<OrderMessage>(body);

        // Your business logic here
        await _orderService.ProcessOrderAsync(orderMessage);

        _logger.Info($"Processed order: {orderMessage.OrderId}");
    }
}
```

#### Custom Connection Settings

```csharp
public class CustomConsumer : RabbitMQBaseConsumer
{
    public CustomConsumer() : base("rabbit.company.com", "username", "password")
    {
    }

    protected override string? QueueName => "custom.queue";

    protected override async Task HandleMessageAsync(string body)
    {
        // Your implementation
    }
}
```

### Sender Implementation

#### Service Registration (ASP.NET Core)

```csharp
// Program.cs or Startup.cs
builder.Services.AddSingleton<IRabbitMQSender, RabbitMQSender>();
```

#### Simple Queue Publishing

```csharp
public class EmailService
{
    private readonly IRabbitMQSender _messageSender;

    public EmailService(IRabbitMQSender messageSender)
    {
        _messageSender = messageSender;
    }

    public async Task SendEmailNotificationAsync(EmailMessage message)
    {
        // Publish to simple queue
        await _messageSender.PublishMessage(message, "email.queue");
    }
}
```

#### Exchange-Based Publishing

```csharp
public class OrderService
{
    private readonly IRabbitMQSender _messageSender;

    public OrderService(IRabbitMQSender messageSender)
    {
        _messageSender = messageSender;
    }

    public async Task PublishOrderEventAsync(OrderCreatedMessage message)
    {
        // Publish to multiple queues via exchange
        var queues = new Dictionary<string, string>
        {
            { "order.created", "order.processing" },
            { "order.created", "email.notifications" },
            { "order.created", "inventory.updates" }
        };

        await _messageSender.PublishMessage(message, "order.exchange", queues);
    }
}
```

## ⚙️ Configuration

### Default Connection Settings

- **Host**: localhost
- **Username**: guest
- **Password**: guest
- **Port**: 5672 (default AMQP port)

### Production Configuration

For production environments, use connection parameters from configuration:

```csharp
public class ProductionConsumer : RabbitMQBaseConsumer
{
    public ProductionConsumer(IConfiguration configuration)
        : base(
            configuration["RabbitMQ:HostName"],
            configuration["RabbitMQ:Username"],
            configuration["RabbitMQ:Password"])
    {
    }

    // Implementation...
}
```

### Logging Configuration

The library uses log4net for structured logging. Configure in `log4net.config`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<log4net>
  <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender">
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date [%thread] %-5level %logger - %message%newline" />
    </layout>
  </appender>

  <root>
    <level value="INFO" />
    <appender-ref ref="ConsoleAppender" />
  </root>
</log4net>
```

## 🛡️ Error Handling & Reliability

### Consumer Error Handling

- **Connection Failures**: Automatically logged with full context
- **Message Processing Errors**: Logged without stopping the consumer
- **Resource Management**: Proper disposal of connections and channels
- **Graceful Shutdown**: Supports cancellation tokens for clean shutdown

### Sender Error Handling

- **Thread Safety**: Semaphore-based connection management
- **Connection Recovery**: Automatic connection recreation on failures
- **Resource Cleanup**: Proper disposal patterns implemented

### Production Considerations

- **Manual Acknowledgment**: Messages are acknowledged only after successful processing
- **Dead Letter Queues**: Consider implementing for failed message handling
- **Monitoring**: Comprehensive logging for operational visibility
- **Security**: Use secure credentials and connection encryption in production

## 📋 Examples in Codebase

### Current Implementations

- **Mango.Services.EmailAPI**:

  - `RabbitMQAuthConsumer` (Simple Queue)
  - `RabbitMQCartConsumer` (Simple Queue)

- **Mango.Services.OrderAPI**:

  - `RabbitMQOrderConsumer` (Exchange Pattern)

- **Mango.Services.RewardAPI**:
  - `RabbitMQRewardConsumer` (Exchange Pattern)

## 🔄 Migration Guide

### From Local Base Consumers

1. **Add Project Reference**:

   ```xml
   <ProjectReference Include="../Mango.MessageRabbit/Mango.Message.RabbitMQ.csproj" />
   ```

2. **Update Using Statements**:

   ```csharp
   using Mango.Message.RabbitMQ.Consumer.Base;
   ```

3. **Update Method Signatures**:

   ```csharp
   // Before
   protected override void HandleMessage(string body)

   // After
   protected override async Task HandleMessageAsync(string body)
   ```

4. **Remove Local Files**:
   - Delete local base consumer implementations
   - Remove redundant connection management code

## 📈 Benefits

### For Development Teams

- **Consistency**: Unified approach across all services
- **Maintainability**: Single source of truth for RabbitMQ operations
- **Documentation**: Comprehensive XML documentation and code examples
- **Developer Experience**: Rich IntelliSense and clear error messages

### for Production Operations

- **Reliability**: Robust error handling and resource management
- **Observability**: Structured logging with operational context
- **Scalability**: Thread-safe implementations support high-throughput scenarios
- **Security**: Production-ready configuration patterns

## 📚 Documentation

- **XML Documentation**: Complete API documentation with examples
- **Code Comments**: Professional inline documentation
- **Region Organization**: Well-structured code with logical grouping
- **Migration Guides**: Step-by-step migration instructions

## 🏷️ Version History

### Current Version

- Enhanced XML documentation and code organization
- Migrated from Console.WriteLine to log4net
- Professional comment standards and cross-referencing
- Production-ready error handling and resource management

---

_Last Updated: August 2025_  
_Build Status: ✅ Success_
