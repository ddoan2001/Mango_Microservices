using log4net;
using Mango.Message.RabbitMQ.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Message.RabbitMQ.Consumer.Base
{
    /// <summary>
    /// Abstract base class for RabbitMQ message consumers.
    /// Supports both simple queue-based consumption and exchange-based consumption with routing keys.
    /// Inherits from BackgroundService to run as a hosted service in ASP.NET Core applications.
    /// </summary>
    public abstract class RabbitMQBaseConsumer : BackgroundService
    {
        #region Connection Configuration Fields
        /// <summary>
        /// RabbitMQ server hostname (e.g., "localhost", "rabbit.company.com").
        /// </summary>
        private readonly string _hostName;

        /// <summary>
        /// RabbitMQ server port. Default is 5672 for standard AMQP connections.
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// RabbitMQ username for authentication. Default is "guest" for local development.
        /// </summary>
        private readonly string _username;

        /// <summary>
        /// RabbitMQ password for authentication. Should be secure in production environments.
        /// </summary>
        private readonly string _password;

        /// <summary>
        /// Virtual host for RabbitMQ connection. Default is "/" (root virtual host).
        /// </summary>
        private readonly string _virtualHost;

        /// <summary>
        /// Whether to use SSL/TLS for the connection. Default is false for development.
        /// </summary>
        private readonly bool _useSsl;

        /// <summary>
        /// Connection timeout in milliseconds. Default is 30000 (30 seconds).
        /// </summary>
        private readonly int _connectionTimeoutMs;

        /// <summary>
        /// Heartbeat interval in seconds. Default is 60 seconds.
        /// </summary>
        private readonly ushort _heartbeatInterval;

        #endregion

        #region Abstract Configuration Properties
        /// <summary>
        /// Gets the queue name for simple queue-based consumption.
        /// Override this property to specify the target queue name.
        /// Used when consuming messages directly from a queue without exchanges.
        /// </summary>
        /// <remarks>
        /// When using simple queue consumption, only this property needs to be overridden.
        /// The queue will be declared with default settings (non-durable, non-exclusive, non-auto-delete).
        /// </remarks>
        protected virtual string? QueueName => null;

        /// <summary>
        /// Gets the exchange name for exchange-based consumption.
        /// Override this property along with <see cref="Queue"/> to specify the target exchange.
        /// Used when consuming messages through an exchange with routing keys.
        /// </summary>
        /// <remarks>
        /// When using exchange-based consumption, both this property and <see cref="Queue"/> must be overridden.
        /// The exchange will be declared as a direct exchange with non-durable settings.
        /// </remarks>
        protected virtual string? ExchangeName => null;

        /// <summary>
        /// Gets the queue configuration for exchange-based consumption.
        /// The Key represents the routing key, and the Value represents the queue name.
        /// Override this property along with <see cref="ExchangeName"/> for exchange-based consumption.
        /// </summary>
        /// <remarks>
        /// Example: new KeyValuePair&lt;string, string&gt;("order.created", "order-processing-queue")
        /// This creates a binding where messages with routing key "order.created" are routed to "order-processing-queue".
        /// </remarks>
        protected virtual KeyValuePair<string, string> Queue => default;

        /// <summary>
        /// Gets a value indicating whether Dead Letter Queue (DLQ) handling is enabled.
        /// Override this property to enable DLQ functionality for failed message processing.
        /// </summary>
        /// <remarks>
        /// When enabled, failed messages will be routed to a dead letter exchange and queue
        /// instead of being lost or infinitely retried. Default is true for production safety.
        /// </remarks>
        protected virtual bool EnableDeadLetterQueue => true;

        /// <summary>
        /// Gets the maximum number of retry attempts before sending a message to the Dead Letter Queue.
        /// Override this property to customize retry behavior for your specific use case.
        /// </summary>
        /// <remarks>
        /// Messages will be retried this many times before being sent to the DLQ.
        /// Set to 0 to disable retries and send failed messages directly to DLQ.
        /// Default is 3 retries, which is suitable for most production scenarios.
        /// </remarks>
        protected virtual int MaxRetryAttempts => 3;

        /// <summary>
        /// Gets the delay in milliseconds between retry attempts.
        /// Override this property to customize retry timing for your specific use case.
        /// </summary>
        /// <remarks>
        /// This delay helps prevent overwhelming downstream services during temporary failures.
        /// Uses exponential backoff: delay * (attempt^2) for progressive delays.
        /// Default is 5000ms (5 seconds), which provides reasonable spacing between retries.
        /// </remarks>
        protected virtual int RetryDelayMilliseconds => 5000;

        /// <summary>
        /// Logger instance for this class using log4net for structured logging.
        /// </summary>
        /// <remarks>
        /// Static logger instance shared across all instances of this class for performance optimization.
        /// Configured to use the class type as logger name for better log categorization.
        /// </remarks>
        private static readonly ILog _logger = LogManager.GetLogger(typeof(RabbitMQBaseConsumer));
        #endregion

        #region RabbitMQ Infrastructure Fields

        /// <summary>
        /// RabbitMQ connection instance for this consumer.
        /// Manages the TCP connection to the RabbitMQ server and handles connection recovery.
        /// </summary>
        private IConnection? _connection;

        /// <summary>
        /// RabbitMQ channel instance for message consumption.
        /// Provides the communication channel for declaring queues, exchanges, and consuming messages.
        /// Each consumer should use its own dedicated channel for thread safety.
        /// </summary>
        private IChannel? _channel;

        /// <summary>
        /// Dead Letter Queue name for failed messages.
        /// Automatically generated based on the main queue name with ".dlq" suffix.
        /// </summary>
        private string? _deadLetterQueueName;

        /// <summary>
        /// Dead Letter Exchange name for routing failed messages.
        /// Automatically generated based on the main exchange name with ".dlx" suffix.
        /// </summary>
        private string? _deadLetterExchangeName;

        #endregion

        #region Health Monitoring Fields

        /// <summary>
        /// Current health status of the RabbitMQ consumer.
        /// Used for health checks and monitoring integration.
        /// </summary>
        private HealthStatus _healthStatus = HealthStatus.Unhealthy;

        /// <summary>
        /// Last successful message processing timestamp.
        /// Used to detect if the consumer is actively processing messages.
        /// </summary>
        private DateTime? _lastSuccessfulProcessing;

        /// <summary>
        /// Last health check timestamp for tracking health monitoring frequency.
        /// </summary>
        private DateTime _lastHealthCheck = DateTime.UtcNow;

        /// <summary>
        /// Counter for successful message processing.
        /// Used for health monitoring and metrics collection.
        /// </summary>
        private long _successfulMessageCount = 0;

        /// <summary>
        /// Counter for failed message processing.
        /// Used for health monitoring and metrics collection.
        /// </summary>
        private long _failedMessageCount = 0;

        /// <summary>
        /// Lock object for thread-safe health status updates.
        /// </summary>
        private readonly object _healthLock = new object();

        #endregion

        #region Health Monitoring Properties and Methods

        /// <summary>
        /// Gets the current health status of the RabbitMQ consumer.
        /// Can be used by health check systems and monitoring tools.
        /// </summary>
        public HealthStatus HealthStatus
        {
            get
            {
                lock (_healthLock)
                {
                    return _healthStatus;
                }
            }
        }

        /// <summary>
        /// Gets the last successful message processing timestamp.
        /// Returns null if no messages have been successfully processed yet.
        /// </summary>
        public DateTime? LastSuccessfulProcessing
        {
            get
            {
                lock (_healthLock)
                {
                    return _lastSuccessfulProcessing;
                }
            }
        }

        /// <summary>
        /// Gets the total number of successfully processed messages.
        /// </summary>
        public long SuccessfulMessageCount
        {
            get
            {
                lock (_healthLock)
                {
                    return _successfulMessageCount;
                }
            }
        }

        /// <summary>
        /// Gets the total number of failed message processing attempts.
        /// </summary>
        public long FailedMessageCount
        {
            get
            {
                lock (_healthLock)
                {
                    return _failedMessageCount;
                }
            }
        }

        /// <summary>
        /// Gets detailed health information for the RabbitMQ consumer.
        /// Includes connection status, message processing metrics, and diagnostic information.
        /// </summary>
        public RabbitMQHealthInfo GetHealthInfo()
        {
            lock (_healthLock)
            {
                return new RabbitMQHealthInfo
                {
                    HealthStatus = _healthStatus,
                    ConsumerName = this.GetType().Name,
                    IsConnectionOpen = _connection?.IsOpen ?? false,
                    IsChannelOpen = _channel?.IsOpen ?? false,
                    QueueName = QueueName ?? Queue.Value,
                    ExchangeName = ExchangeName,
                    LastSuccessfulProcessing = _lastSuccessfulProcessing,
                    LastHealthCheck = _lastHealthCheck,
                    SuccessfulMessageCount = _successfulMessageCount,
                    FailedMessageCount = _failedMessageCount,
                    DeadLetterQueueEnabled = EnableDeadLetterQueue,
                    MaxRetryAttempts = MaxRetryAttempts,
                    RetryDelayMilliseconds = RetryDelayMilliseconds
                };
            }
        }

        /// <summary>
        /// Checks and updates the health status of the RabbitMQ consumer.
        /// Called periodically to assess connection health and processing activity.
        /// </summary>
        /// <returns>The current health status after the check</returns>
        public HealthStatus CheckHealth()
        {
            lock (_healthLock)
            {
                _lastHealthCheck = DateTime.UtcNow;

                try
                {
                    // Check connection and channel status
                    bool isConnectionHealthy = _connection?.IsOpen == true;
                    bool isChannelHealthy = _channel?.IsOpen == true;

                    if (!isConnectionHealthy || !isChannelHealthy)
                    {
                        _healthStatus = HealthStatus.Unhealthy;
                        _logger.Warn($"[RabbitMQBaseConsumer] Health check failed - Connection: {isConnectionHealthy}, Channel: {isChannelHealthy}");
                        return _healthStatus;
                    }

                    // Check if messages are being processed (within last 5 minutes for active systems)
                    var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
                    bool recentlyProcessedMessages = _lastSuccessfulProcessing.HasValue &&
                                                    _lastSuccessfulProcessing.Value > fiveMinutesAgo;

                    // Calculate failure rate
                    double totalMessages = _successfulMessageCount + _failedMessageCount;
                    double failureRate = totalMessages > 0 ? (_failedMessageCount / totalMessages) : 0;

                    if (failureRate > 0.1) // More than 10% failure rate
                    {
                        _healthStatus = HealthStatus.Degraded;
                        _logger.Warn($"[RabbitMQBaseConsumer] Health degraded - High failure rate: {failureRate:P2}");
                    }
                    else if (isConnectionHealthy && isChannelHealthy)
                    {
                        _healthStatus = HealthStatus.Healthy;
                    }
                    else
                    {
                        _healthStatus = HealthStatus.Degraded;
                    }

                    _logger.Debug($"[RabbitMQBaseConsumer] Health check completed - Status: {_healthStatus}");
                    return _healthStatus;
                }
                catch (Exception ex)
                {
                    _healthStatus = HealthStatus.Unhealthy;
                    _logger.Error($"[RabbitMQBaseConsumer] Health check failed with exception: {ex.Message}", ex);
                    return _healthStatus;
                }
            }
        }

        /// <summary>
        /// Updates health status after successful message processing.
        /// Called internally when messages are processed successfully.
        /// </summary>
        private void UpdateHealthOnSuccess()
        {
            lock (_healthLock)
            {
                _successfulMessageCount++;
                _lastSuccessfulProcessing = DateTime.UtcNow;

                // If connection and channel are healthy, mark as healthy
                if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                {
                    _healthStatus = HealthStatus.Healthy;
                }
            }
        }

        /// <summary>
        /// Updates health status after failed message processing.
        /// Called internally when message processing fails.
        /// </summary>
        private void UpdateHealthOnFailure()
        {
            lock (_healthLock)
            {
                _failedMessageCount++;

                // Calculate failure rate to determine health status
                double totalMessages = _successfulMessageCount + _failedMessageCount;
                double failureRate = _failedMessageCount / totalMessages;

                if (failureRate > 0.2) // More than 20% failure rate
                {
                    _healthStatus = HealthStatus.Unhealthy;
                }
                else if (failureRate > 0.1) // More than 10% failure rate
                {
                    _healthStatus = HealthStatus.Degraded;
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQBaseConsumer"/> class with default connection settings.
        /// Uses localhost with guest credentials, suitable for local development and testing.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when neither queue configuration is properly set in derived classes.
        /// </exception>
        /// <remarks>
        /// Connection creation is deferred until ExecuteAsync to ensure derived class constructors
        /// have completed initialization before accessing virtual properties.
        /// </remarks>
        public RabbitMQBaseConsumer()
        {
            _hostName = "localhost";
            _port = 5672;
            _username = "guest";
            _password = "guest";
            _virtualHost = "/";
            _useSsl = false;
            _connectionTimeoutMs = 30000;
            _heartbeatInterval = 60;

            _logger.Info("[RabbitMQBaseConsumer] Initialized with default settings (localhost:guest)");
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitMQBaseConsumer"/> with configuration options.
        /// Recommended for production environments where configuration is managed via appsettings.json or similar.
        /// </summary>
        /// <param name="connectionOptions">RabbitMQ connection configuration options</param>
        /// <exception cref="ArgumentNullException">Thrown when connectionOptions is null</exception>
        /// <remarks>
        /// This constructor allows for dependency injection of configuration options from appsettings.json
        /// or other configuration sources. Connection creation is deferred until ExecuteAsync to ensure
        /// derived class constructors have completed initialization before accessing virtual properties.
        /// </remarks>
        public RabbitMQBaseConsumer(RabbitMQConnectionOptions connectionOptions)
        {
            if (connectionOptions == null)
                throw new ArgumentNullException(nameof(connectionOptions));

            _hostName = connectionOptions.HostName;
            _port = connectionOptions.Port;
            _username = connectionOptions.Username;
            _password = connectionOptions.Password;
            _virtualHost = connectionOptions.VirtualHost;
            _useSsl = connectionOptions.UseSsl;
            _connectionTimeoutMs = connectionOptions.ConnectionTimeoutMs;
            _heartbeatInterval = connectionOptions.HeartbeatInterval;

            _logger.Info($"[RabbitMQBaseConsumer] Initialized with configuration options ({connectionOptions.HostName}:{connectionOptions.Username})");
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Creates and configures the RabbitMQ connection and channel infrastructure.
        /// Automatically determines the consumption pattern (simple queue vs. exchange-based) based on 
        /// overridden properties and sets up the appropriate RabbitMQ infrastructure.
        /// </summary>
        /// <returns>A task representing the asynchronous connection creation operation</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when configuration properties are invalid or incompatible
        /// </exception>
        /// <remarks>
        /// This method supports two consumption patterns:
        /// 1. Simple Queue: Override <see cref="QueueName"/> only
        /// 2. Exchange-based: Override both <see cref="ExchangeName"/> and <see cref="Queue"/>
        /// 
        /// All infrastructure (exchanges, queues, bindings) is created with non-durable settings
        /// suitable for development. For production, consider implementing durable configurations.
        /// </remarks>
        private async Task CreateConnectionAsync()
        {
            try
            {
                _logger.Info("[RabbitMQBaseConsumer] Initializing RabbitMQ connection...");

                #region Connection Factory Configuration

                // Create connection factory with provided credentials and configuration
                var factory = new ConnectionFactory
                {
                    HostName = _hostName,
                    Port = _port,
                    UserName = _username,
                    Password = _password,
                    VirtualHost = _virtualHost,
                    RequestedConnectionTimeout = TimeSpan.FromMilliseconds(_connectionTimeoutMs),
                    RequestedHeartbeat = TimeSpan.FromSeconds(_heartbeatInterval)
                };

                // Configure SSL if enabled
                if (_useSsl)
                {
                    factory.Ssl.Enabled = true;
                    factory.Ssl.ServerName = _hostName;
                }

                #endregion

                #region Connection and Channel Creation

                // Establish connection to RabbitMQ server
                _connection = await factory.CreateConnectionAsync();

                // Create a dedicated channel for this consumer
                _channel = await _connection.CreateChannelAsync();

                // Update health status after successful connection
                lock (_healthLock)
                {
                    _healthStatus = HealthStatus.Healthy;
                }

                _logger.Info("[RabbitMQBaseConsumer] Connection and channel created successfully");

                #endregion

                #region Infrastructure Setup Based on Configuration

                // Determine consumption pattern and set up appropriate infrastructure
                if (!string.IsNullOrEmpty(ExchangeName) && !Queue.Equals(default(KeyValuePair<string, string>)))
                {
                    #region Exchange-Based Pattern Setup

                    _logger.Debug($"[RabbitMQBaseConsumer] Setting up exchange-based consumption - Exchange: {ExchangeName}, Queue: {Queue.Value}, Routing Key: {Queue.Key}");

                    // Setup Dead Letter Queue infrastructure if enabled
                    if (EnableDeadLetterQueue)
                    {
                        await SetupDeadLetterQueueAsync(ExchangeName, Queue.Value);
                    }

                    // Declare the exchange
                    await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: false);

                    // Prepare queue arguments for DLQ support
                    Dictionary<string, object?>? queueArguments = null;
                    if (EnableDeadLetterQueue && !string.IsNullOrEmpty(_deadLetterExchangeName))
                    {
                        queueArguments = new Dictionary<string, object?>
                        {
                            { "x-dead-letter-exchange", _deadLetterExchangeName },
                            { "x-dead-letter-routing-key", $"{Queue.Key}.failed" }
                        };
                        _logger.Debug($"[RabbitMQBaseConsumer] Queue configured with DLQ support - DLX: {_deadLetterExchangeName}");
                    }

                    // Declare the queue with DLQ arguments
                    await _channel.QueueDeclareAsync(Queue.Value, durable: false, exclusive: false, autoDelete: false, arguments: queueArguments);

                    // Bind queue to exchange with routing key
                    await _channel.QueueBindAsync(Queue.Value, ExchangeName, Queue.Key);

                    _logger.Info("[RabbitMQBaseConsumer] Exchange-based infrastructure setup completed");

                    #endregion
                }
                else if (!string.IsNullOrEmpty(QueueName))
                {
                    #region Simple Queue-Based Pattern Setup

                    _logger.Debug($"[RabbitMQBaseConsumer] Setting up simple queue consumption - Queue: {QueueName}");

                    // Setup Dead Letter Queue infrastructure if enabled
                    if (EnableDeadLetterQueue)
                    {
                        await SetupDeadLetterQueueAsync("", QueueName);
                    }

                    // Prepare queue arguments for DLQ support
                    Dictionary<string, object?>? queueArguments = null;
                    if (EnableDeadLetterQueue && !string.IsNullOrEmpty(_deadLetterExchangeName))
                    {
                        queueArguments = new Dictionary<string, object?>
                        {
                            { "x-dead-letter-exchange", _deadLetterExchangeName },
                            { "x-dead-letter-routing-key", $"{QueueName}.failed" }
                        };
                        _logger.Debug($"[RabbitMQBaseConsumer] Queue configured with DLQ support - DLX: {_deadLetterExchangeName}");
                    }

                    // Declare the queue for direct consumption with DLQ arguments
                    await _channel.QueueDeclareAsync(QueueName, durable: false, exclusive: false, autoDelete: false, arguments: queueArguments);

                    _logger.Info("[RabbitMQBaseConsumer] Simple queue infrastructure setup completed");

                    #endregion
                }
                else
                {
                    #region Configuration Validation

                    var errorMessage = "Invalid consumer configuration: Either QueueName must be provided for simple queue consumption, " +
                                     "or both ExchangeName and Queue must be provided for exchange-based consumption.";

                    _logger.Error($"[RabbitMQBaseConsumer] {errorMessage}");
                    throw new InvalidOperationException(errorMessage);

                    #endregion
                }

                #endregion

                _logger.Info("[RabbitMQBaseConsumer] RabbitMQ consumer initialization completed successfully");
            }
            catch (Exception ex)
            {
                // Enhanced error logging with full context for troubleshooting
                var errorMessage = $"Failed to create RabbitMQ connection and setup infrastructure: {ex.Message}";
                _logger.Error($"[RabbitMQBaseConsumer] {errorMessage}", ex);

                // Re-throw to allow calling code to handle the failure appropriately
                throw;
            }
        }

        /// <summary>
        /// Sets up Dead Letter Queue infrastructure for handling failed messages.
        /// Creates the dead letter exchange and queue with appropriate bindings.
        /// </summary>
        /// <param name="originalExchangeName">The original exchange name (empty for simple queues)</param>
        /// <param name="originalQueueName">The original queue name</param>
        /// <returns>A task representing the asynchronous DLQ setup operation</returns>
        /// <remarks>
        /// This method creates:
        /// 1. Dead Letter Exchange (DLX) with ".dlx" suffix
        /// 2. Dead Letter Queue (DLQ) with ".dlq" suffix  
        /// 3. Binding between DLX and DLQ for failed message routing
        /// 
        /// Failed messages will be routed to the DLQ after max retry attempts are exceeded.
        /// </remarks>
        private async Task SetupDeadLetterQueueAsync(string originalExchangeName, string originalQueueName)
        {
            try
            {
                if (_channel == null)
                    throw new InvalidOperationException("RabbitMQ channel is not initialized");

                // Generate DLQ names based on original queue/exchange
                if (!string.IsNullOrEmpty(originalExchangeName))
                {
                    _deadLetterExchangeName = $"{originalExchangeName}.dlx";
                    _deadLetterQueueName = $"{originalQueueName}.dlq";
                }
                else
                {
                    _deadLetterExchangeName = $"{originalQueueName}.dlx";
                    _deadLetterQueueName = $"{originalQueueName}.dlq";
                }

                _logger.Debug($"[RabbitMQBaseConsumer] Setting up DLQ infrastructure - DLX: {_deadLetterExchangeName}, DLQ: {_deadLetterQueueName}");

                // Declare Dead Letter Exchange
                await _channel.ExchangeDeclareAsync(_deadLetterExchangeName, ExchangeType.Direct, durable: false);

                // Declare Dead Letter Queue
                await _channel.QueueDeclareAsync(_deadLetterQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

                // Bind DLQ to DLX with appropriate routing key
                var dlqRoutingKey = !string.IsNullOrEmpty(originalExchangeName)
                    ? $"{Queue.Key}.failed"
                    : $"{originalQueueName}.failed";

                await _channel.QueueBindAsync(_deadLetterQueueName, _deadLetterExchangeName, dlqRoutingKey);

                _logger.Info($"[RabbitMQBaseConsumer] DLQ infrastructure setup completed - DLQ: {_deadLetterQueueName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[RabbitMQBaseConsumer] Failed to setup DLQ infrastructure: {ex.Message}", ex);
                throw new InvalidOperationException($"Failed to setup Dead Letter Queue: {ex.Message}", ex);
            }
        }

        #endregion

        #region Message Processing with Retry Logic

        /// <summary>
        /// Processes a message with retry logic and Dead Letter Queue handling.
        /// Implements exponential backoff retry strategy for transient failures.
        /// </summary>
        /// <param name="body">The message body to process</param>
        /// <param name="deliveryTag">The message delivery tag for acknowledgment</param>
        /// <param name="routingKey">The routing key for DLQ routing</param>
        /// <returns>A task representing the asynchronous message processing operation</returns>
        private async Task ProcessMessageWithRetryAsync(string body, ulong deliveryTag, string routingKey)
        {
            if (_channel == null)
                throw new InvalidOperationException("RabbitMQ channel is not initialized");

            var attempt = 0;
            Exception? lastException = null;

            while (attempt <= MaxRetryAttempts)
            {
                try
                {
                    _logger.Debug($"[RabbitMQBaseConsumer] Processing message attempt {attempt + 1}/{MaxRetryAttempts + 1}, DeliveryTag: {deliveryTag}");

                    // Process the message using the derived class implementation
                    await HandleMessageAsync(body);

                    // Acknowledge successful message processing
                    await _channel.BasicAckAsync(deliveryTag, multiple: false);

                    // Update health monitoring for successful processing
                    UpdateHealthOnSuccess();

                    _logger.Debug($"[RabbitMQBaseConsumer] Successfully processed and acknowledged message: {deliveryTag}");
                    return; // Success - exit retry loop
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;

                    _logger.Warn($"[RabbitMQBaseConsumer] Message processing failed (attempt {attempt}/{MaxRetryAttempts + 1}), DeliveryTag: {deliveryTag}, Error: {ex.Message}");

                    if (attempt <= MaxRetryAttempts)
                    {
                        // Calculate exponential backoff delay
                        var delay = RetryDelayMilliseconds * (int)Math.Pow(2, attempt - 1);
                        _logger.Debug($"[RabbitMQBaseConsumer] Retrying in {delay}ms...");

                        await Task.Delay(delay);
                    }
                }
            }

            // All retries exhausted - handle failed message
            UpdateHealthOnFailure();
            await HandleFailedMessageAsync(body, deliveryTag, routingKey, lastException!);
        }

        /// <summary>
        /// Handles a message that failed processing after all retry attempts.
        /// Routes the message to Dead Letter Queue if enabled, otherwise rejects it.
        /// </summary>
        /// <param name="body">The failed message body</param>
        /// <param name="deliveryTag">The message delivery tag</param>
        /// <param name="routingKey">The original routing key</param>
        /// <param name="exception">The exception that caused the failure</param>
        /// <returns>A task representing the asynchronous failed message handling operation</returns>
        private async Task HandleFailedMessageAsync(string body, ulong deliveryTag, string routingKey, Exception exception)
        {
            if (_channel == null)
                throw new InvalidOperationException("RabbitMQ channel is not initialized");

            try
            {
                if (EnableDeadLetterQueue && !string.IsNullOrEmpty(_deadLetterExchangeName))
                {
                    _logger.Error($"[RabbitMQBaseConsumer] Message failed all retry attempts, sending to DLQ - DeliveryTag: {deliveryTag}, Error: {exception.Message}", exception);

                    // Publish message to Dead Letter Queue with failure metadata
                    var failedMessage = new
                    {
                        OriginalBody = body,
                        OriginalRoutingKey = routingKey,
                        FailureReason = exception.Message,
                        FailureStackTrace = exception.StackTrace,
                        FailedAt = DateTime.UtcNow,
                        RetryAttempts = MaxRetryAttempts,
                        ConsumerType = this.GetType().Name
                    };

                    var messageJson = JsonConvert.SerializeObject(failedMessage);
                    var messageBytes = Encoding.UTF8.GetBytes(messageJson);

                    // Send to DLQ with failure routing key
                    var dlqRoutingKey = $"{routingKey}.failed";
                    await _channel.BasicPublishAsync(_deadLetterExchangeName, dlqRoutingKey, false, messageBytes);

                    // Acknowledge the original message to remove it from the main queue
                    await _channel.BasicAckAsync(deliveryTag, multiple: false);

                    _logger.Info($"[RabbitMQBaseConsumer] Failed message sent to DLQ successfully - DeliveryTag: {deliveryTag}");
                }
                else
                {
                    _logger.Error($"[RabbitMQBaseConsumer] Message failed all retry attempts, DLQ disabled - rejecting message - DeliveryTag: {deliveryTag}, Error: {exception.Message}", exception);

                    // Reject the message without requeuing since we can't process it
                    await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[RabbitMQBaseConsumer] Failed to handle failed message - DeliveryTag: {deliveryTag}, Error: {ex.Message}", ex);

                // As last resort, reject the message to prevent infinite blocking
                await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
            }
        }

        #endregion

        #region BackgroundService Implementation

        /// <summary>
        /// Executes the background message consumption task as part of the hosted service lifecycle.
        /// Sets up the message consumer and begins listening for incoming messages from the configured queue.
        /// This method runs continuously until the service is stopped or cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">
        /// Cancellation token to monitor for stop requests from the host application
        /// </param>
        /// <returns>A task that represents the background execution operation</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the RabbitMQ channel is not properly initialized
        /// </exception>
        /// <remarks>
        /// This method implements the BackgroundService.ExecuteAsync override and handles:
        /// - Setting up the asynchronous message consumer
        /// - Processing incoming messages through the abstract <see cref="HandleMessageAsync"/> method
        /// - Manual message acknowledgment for reliable message processing
        /// - Graceful shutdown when cancellation is requested
        /// 
        /// Messages are processed with manual acknowledgment (autoAck: false) to ensure reliability.
        /// Failed message processing is logged but does not stop the consumer.
        /// </remarks>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            #region Connection Initialization

            // Create RabbitMQ connection and channel infrastructure
            // This is done here instead of constructor to ensure derived class constructors
            // have completed and virtual properties are properly initialized
            _logger.Info("[RabbitMQBaseConsumer] Starting RabbitMQ consumer execution...");
            await CreateConnectionAsync();

            #endregion

            #region Pre-execution Validation

            // Check if cancellation was requested before starting
            stoppingToken.ThrowIfCancellationRequested();

            // Validate that the channel was successfully created during initialization
            if (_channel == null)
                throw new InvalidOperationException("RabbitMQ channel is not initialized. Connection setup may have failed.");

            #endregion

            #region Message Consumer Setup

            _logger.Debug("[RabbitMQBaseConsumer] Setting up message consumer...");

            // Create an asynchronous consumer for handling messages
            var consumer = new AsyncEventingBasicConsumer(_channel);

            // Configure the message received event handler with error handling and DLQ support
            consumer.ReceivedAsync += async (ch, ea) =>
            {
                // Convert message body from bytes to UTF-8 string
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var routingKey = ea.RoutingKey ?? "";

                _logger.Debug($"[RabbitMQBaseConsumer] Received message with delivery tag: {ea.DeliveryTag}");

                // Process message with retry logic and DLQ handling
                await ProcessMessageWithRetryAsync(body, ea.DeliveryTag, routingKey);
            };

            #endregion

            #region Message Consumption Activation

            // Determine which queue to consume from based on configuration pattern
            var queueToConsume = !string.IsNullOrEmpty(QueueName) ? QueueName : Queue.Value;

            _logger.Info($"[RabbitMQBaseConsumer] Starting message consumption from queue: {queueToConsume}");

            // Start consuming messages with manual acknowledgment for reliability
            // autoAck = false ensures messages aren't removed until explicitly acknowledged
            await _channel.BasicConsumeAsync(queueToConsume, autoAck: false, consumer: consumer);

            _logger.Info("[RabbitMQBaseConsumer] Message consumption started successfully");

            #endregion

            #region Service Lifecycle Management

            // Keep the service running until cancellation is requested
            // This allows the message consumer to process messages continuously
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Info("[RabbitMQBaseConsumer] Consumption cancelled - shutting down gracefully");
            }

            #endregion
        }

        #endregion

        #region Abstract Contract

        /// <summary>
        /// Handles incoming messages from RabbitMQ queue or exchange.
        /// This method must be implemented by derived classes to define specific message processing logic
        /// appropriate for their business domain (e.g., order processing, email sending, reward calculation).
        /// </summary>
        /// <param name="body">
        /// The message body as a UTF-8 encoded string. The content format depends on the message producer
        /// and may be JSON, XML, plain text, or other formats as defined by your messaging contract.
        /// </param>
        /// <returns>A task representing the asynchronous message handling operation</returns>
        /// <remarks>
        /// <para>
        /// This method is called for each message received from the queue or exchange.
        /// Implementation guidelines:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Parse the message body according to your expected format</description></item>
        /// <item><description>Perform business logic processing</description></item>
        /// <item><description>Handle errors gracefully - exceptions will be logged but won't stop the consumer</description></item>
        /// <item><description>Avoid long-running operations that could block other messages</description></item>
        /// <item><description>Consider implementing idempotency for reliable processing</description></item>
        /// </list>
        /// <para>
        /// Message acknowledgment is handled automatically by the base class upon successful completion.
        /// If this method throws an exception, the message will not be acknowledged and may be redelivered.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// protected override async Task HandleMessageAsync(string body)
        /// {
        ///     var orderMessage = JsonSerializer.Deserialize&lt;OrderCreatedMessage&gt;(body);
        ///     await _orderService.ProcessOrderAsync(orderMessage.OrderId);
        ///     _logger.Info($"Processed order: {orderMessage.OrderId}");
        /// }
        /// </code>
        /// </example>
        protected abstract Task HandleMessageAsync(string body);

        #endregion

        #region Resource Management

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// Properly disposes of RabbitMQ connections and channels to prevent resource leaks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method ensures proper cleanup of RabbitMQ resources in the following order:
        /// </para>
        /// <list type="number">
        /// <item><description>Dispose the channel (closes message consumption)</description></item>
        /// <item><description>Dispose the connection (closes TCP connection to RabbitMQ)</description></item>
        /// <item><description>Call base.Dispose() to clean up BackgroundService resources</description></item>
        /// </list>
        /// <para>
        /// The disposal process is protected against exceptions to ensure that all cleanup steps
        /// are attempted even if individual steps fail. Any errors during disposal are logged
        /// but do not prevent the cleanup process from completing.
        /// </para>
        /// </remarks>
        public override void Dispose()
        {
            try
            {
                _logger.Debug("[RabbitMQBaseConsumer] Disposing RabbitMQ resources...");

                // Dispose channel first to stop message consumption
                _channel?.Dispose();

                // Then dispose connection to close TCP connection
                _connection?.Dispose();

                _logger.Info("[RabbitMQBaseConsumer] RabbitMQ resources disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.Error("[RabbitMQBaseConsumer] Error during disposal", ex);
            }
            finally
            {
                // Always call base dispose to ensure proper cleanup of BackgroundService
                // This ensures the hosted service is properly removed from the service collection
                base.Dispose();
            }
        }

        #endregion
    }
}
