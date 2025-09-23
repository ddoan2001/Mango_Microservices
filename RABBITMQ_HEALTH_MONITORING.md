# RabbitMQ Consumer Health Monitoring Implementation

## 🏥 **Overview**

Implemented comprehensive health monitoring for RabbitMQ consumers to provide visibility into connection status, message processing metrics, and overall system health.

## 🚀 **Features Implemented**

### 1. **Base Consumer Health Monitoring**

- **Connection Health**: Monitors RabbitMQ connection and channel status
- **Message Processing Metrics**: Tracks successful and failed message counts
- **Failure Rate Calculation**: Automatic calculation of message processing failure rates
- **Activity Monitoring**: Tracks last successful processing timestamp
- **Thread-Safe Operations**: All health status updates are thread-safe

### 2. **Health Status Levels**

- **Healthy**: Connection open, channel active, low failure rate (<10%)
- **Degraded**: Connection issues or moderate failure rate (10-20%)
- **Unhealthy**: Connection down, channel closed, or high failure rate (>20%)

### 3. **Detailed Health Information**

```csharp
public class RabbitMQHealthInfo
{
    public HealthStatus HealthStatus { get; set; }
    public string ConsumerName { get; set; }
    public bool IsConnectionOpen { get; set; }
    public bool IsChannelOpen { get; set; }
    public DateTime? LastSuccessfulProcessing { get; set; }
    public long SuccessfulMessageCount { get; set; }
    public long FailedMessageCount { get; set; }
    public double FailureRatePercentage { get; }
    public bool IsActivelyProcessing { get; }
    public string HealthSummary { get; }
    // ... additional properties
}
```

## 📊 **Health Monitoring API**

### Health Check Endpoints

#### 1. **Basic Health Check**

```
GET /health
```

**Response:**

```json
{
  "status": "Healthy",
  "totalDuration": 45.2,
  "entries": [
    {
      "name": "default",
      "status": "Healthy",
      "description": "All systems operational",
      "duration": 45.2
    }
  ]
}
```

#### 2. **RabbitMQ Consumer Health**

```
GET /api/health/rabbitmq
```

**Response:**

```json
{
  "overallStatus": "Healthy",
  "totalConsumers": 2,
  "healthyConsumers": 2,
  "degradedConsumers": 0,
  "unhealthyConsumers": 0,
  "consumers": [
    {
      "consumerName": "RabbitMQAuthConsumer",
      "status": "Healthy",
      "connectionOpen": true,
      "channelOpen": true,
      "queueName": "RegisterUserQueue",
      "successfulMessages": 1247,
      "failedMessages": 12,
      "failureRate": "0.95%",
      "lastSuccessfulProcessing": "2024-08-24T10:30:15Z",
      "isActivelyProcessing": true,
      "deadLetterQueueEnabled": true,
      "maxRetryAttempts": 5,
      "retryDelayMilliseconds": 2000,
      "healthSummary": "All systems operational",
      "lastHealthCheck": "2024-08-24T10:35:22Z"
    }
  ],
  "timestamp": "2024-08-24T10:35:22Z"
}
```

#### 3. **Manual Health Check Trigger**

```
POST /api/health/rabbitmq/check
```

**Response:**

```json
{
  "message": "Health check completed",
  "results": [
    {
      "consumerName": "RabbitMQAuthConsumer",
      "status": "Healthy",
      "healthSummary": "All systems operational"
    }
  ],
  "timestamp": "2024-08-24T10:35:22Z"
}
```

## 🔧 **Implementation Details**

### 1. **Base Consumer Integration**

```csharp
public abstract class RabbitMQBaseConsumer : BackgroundService
{
    // Health monitoring properties
    public HealthStatus HealthStatus { get; }
    public DateTime? LastSuccessfulProcessing { get; }
    public long SuccessfulMessageCount { get; }
    public long FailedMessageCount { get; }

    // Health monitoring methods
    public RabbitMQHealthInfo GetHealthInfo()
    public HealthStatus CheckHealth()

    // Internal health updates
    private void UpdateHealthOnSuccess()
    private void UpdateHealthOnFailure()
}
```

### 2. **Automatic Health Updates**

- **Success Events**: Increment success counter, update last processing time, set healthy status
- **Failure Events**: Increment failure counter, calculate failure rate, adjust health status
- **Connection Events**: Update health status based on connection/channel state

### 3. **Health Calculation Logic**

```csharp
// Connection Health
bool isConnectionHealthy = _connection?.IsOpen == true;
bool isChannelHealthy = _channel?.IsOpen == true;

// Failure Rate Assessment
double failureRate = totalMessages > 0 ? (_failedMessageCount / totalMessages) : 0;

// Status Determination
if (failureRate > 0.2) // > 20%
    status = HealthStatus.Unhealthy;
else if (failureRate > 0.1) // > 10%
    status = HealthStatus.Degraded;
else if (isConnectionHealthy && isChannelHealthy)
    status = HealthStatus.Healthy;
```

## 📈 **Monitoring Integration**

### 1. **ASP.NET Core Health Checks**

```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

### 2. **Custom Health Controller**

- Provides detailed RabbitMQ consumer metrics
- Supports manual health check triggers
- Returns comprehensive health summaries

### 3. **Production Monitoring**

```csharp
// Example monitoring setup
services.AddHealthChecks()
    .AddCheck<RabbitMQConsumerHealthCheck>("rabbitmq_consumers")
    .AddCheck("database", () => /* database check */)
    .AddCheck("external_api", () => /* external API check */);
```

## 🎯 **Benefits**

### 1. **Operational Visibility**

- **Real-time Status**: Current health status of all consumers
- **Historical Metrics**: Message processing success/failure rates
- **Performance Insights**: Processing activity and patterns

### 2. **Proactive Monitoring**

- **Early Warning**: Degraded status before complete failure
- **Failure Rate Tracking**: Identify consumers with processing issues
- **Connection Monitoring**: Detect network or RabbitMQ server issues

### 3. **DevOps Integration**

- **Health Check Endpoints**: Standard HTTP endpoints for monitoring tools
- **Load Balancer Integration**: Health checks for service availability
- **Alerting Support**: Structured data for monitoring systems

### 4. **Troubleshooting Support**

- **Detailed Diagnostics**: Connection status, message counts, failure rates
- **Historical Data**: Last processing times and health check timestamps
- **Error Categorization**: Different health statuses for different issue types

## 🔍 **Usage Examples**

### 1. **Monitoring Dashboard Integration**

```javascript
// Fetch health status for dashboard
fetch("/api/health/rabbitmq")
  .then((response) => response.json())
  .then((data) => {
    updateConsumerStatus(data.consumers);
    showOverallHealth(data.overallStatus);
  });
```

### 2. **Alerting Rules**

```yaml
# Prometheus alerting example
- alert: RabbitMQConsumerUnhealthy
  expr: rabbitmq_consumer_status != 1
  for: 5m
  annotations:
    summary: "RabbitMQ consumer {{ $labels.consumer_name }} is unhealthy"
```

### 3. **Load Balancer Health Check**

```nginx
# Nginx upstream health check
upstream emailapi {
    server emailapi1:80;
    server emailapi2:80;
    # Health check endpoint
    health_check uri=/health;
}
```

## 📋 **Implementation Status**

### ✅ **Completed Features**

- Base consumer health monitoring
- Health status calculation
- Message processing metrics
- Connection status tracking
- Health API endpoints
- Thread-safe operations
- Comprehensive health information model

### 🚀 **Next Steps**

- Integration with monitoring systems (Prometheus, Application Insights)
- Health check alerts and notifications
- Performance metrics collection
- Health trend analysis
- Circuit breaker integration

---

_Health monitoring implementation completed: August 2024_  
_Status: ✅ Production ready with comprehensive consumer health monitoring_
