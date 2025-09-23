using log4net;
using Mango.Message.RabbitMQ.Sender.Interface;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Mango.Message.RabbitMQ.Sender
{
    /// <summary>
    /// RabbitMQ message sender implementation that supports both simple queue and exchange-based messaging patterns.
    /// Provides thread-safe, disposable message publishing with automatic connection management and recovery.
    /// </summary>
    public class RabbitMQSender : IRabbitMQSender, IDisposable
    {
        #region Fields and Properties

        /// <summary>
        /// RabbitMQ server hostname
        /// </summary>
        private readonly string _hostName;

        /// <summary>
        /// RabbitMQ username for authentication
        /// </summary>
        private readonly string _username;

        /// <summary>
        /// RabbitMQ password for authentication
        /// </summary>
        private readonly string _password;

        /// <summary>
        /// RabbitMQ connection instance
        /// </summary>
        private IConnection? _connection;

        /// <summary>
        /// Semaphore to ensure thread-safe connection creation
        /// </summary>
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

        /// <summary>
        /// Logger instance for this class
        /// </summary>
        private static readonly ILog _logger = LogManager.GetLogger(typeof(RabbitMQSender));

        /// <summary>
        /// Flag to track if the object has been disposed
        /// </summary>
        private bool _disposed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of RabbitMQSender with default connection settings.
        /// Uses localhost with guest/guest credentials.
        /// </summary>
        public RabbitMQSender()
        {
            _hostName = "localhost";
            _username = "guest";
            _password = "guest";

            _logger.Info("[RabbitMQSender] Initialized with default settings (localhost:guest)");
        }

        /// <summary>
        /// Initializes a new instance of RabbitMQSender with custom connection settings.
        /// </summary>
        /// <param name="hostName">RabbitMQ server hostname</param>
        /// <param name="username">Username for authentication</param>
        /// <param name="password">Password for authentication</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public RabbitMQSender(string hostName, string username, string password)
        {
            _hostName = hostName ?? throw new ArgumentNullException(nameof(hostName));
            _username = username ?? throw new ArgumentNullException(nameof(username));
            _password = password ?? throw new ArgumentNullException(nameof(password));

            _logger.Info($"[RabbitMQSender] Initialized with custom settings ({hostName}:{username})");
        }

        #endregion

        #region Public Message Publishing Methods

        /// <summary>
        /// Publishes a message to a simple queue using the default exchange.
        /// This method creates the queue if it doesn't exist and publishes the message directly to it.
        /// </summary>
        /// <param name="message">The message object to serialize and publish</param>
        /// <param name="queueName">The name of the target queue</param>
        /// <returns>A task representing the asynchronous publish operation</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the sender has been disposed</exception>
        /// <exception cref="ArgumentNullException">Thrown if message is null</exception>
        /// <exception cref="ArgumentException">Thrown if queueName is null or empty</exception>
        public async Task PublishMessage(object message, string queueName)
        {
            #region Input Validation

            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMQSender));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));

            #endregion

            try
            {
                _logger.Debug($"[RabbitMQSender] Publishing message to queue: {queueName}");

                // Ensure we have a valid connection
                await EnsureConnectionAsync();

                // Create channel and declare queue
                using var channel = await _connection!.CreateChannelAsync();
                await channel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

                // Serialize message and publish
                var body = SerializeMessage(message);
                await channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);

                _logger.Info($"[RabbitMQSender] Successfully published message to queue: {queueName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[RabbitMQSender] Error publishing message to queue '{queueName}'", ex);
                throw;
            }
        }

        /// <summary>
        /// Publishes a message to multiple queues through a direct exchange with routing keys.
        /// This method creates the exchange, queues, and bindings if they don't exist.
        /// </summary>
        /// <param name="message">The message object to serialize and publish</param>
        /// <param name="exchangeName">The name of the exchange to publish to</param>
        /// <param name="queues">Dictionary of routing key -> queue name mappings</param>
        /// <returns>A task representing the asynchronous publish operation</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the sender has been disposed</exception>
        /// <exception cref="ArgumentNullException">Thrown if message is null</exception>
        /// <exception cref="ArgumentException">Thrown if exchangeName is null/empty or queues is null/empty</exception>
        public async Task PublishMessage(object message, string exchangeName, Dictionary<string, string> queues)
        {
            #region Input Validation

            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMQSender));

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentException("Exchange name cannot be null or empty", nameof(exchangeName));

            if (queues == null || !queues.Any())
                throw new ArgumentException("Queues dictionary cannot be null or empty", nameof(queues));

            #endregion

            try
            {
                _logger.Debug($"[RabbitMQSender] Publishing message to exchange '{exchangeName}' with {queues.Count} queues");

                // Ensure we have a valid connection
                await EnsureConnectionAsync();

                using var channel = await _connection!.CreateChannelAsync();

                // Declare exchange
                await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: false);

                // Serialize message once for all queues
                var body = SerializeMessage(message);

                // Declare queues, create bindings, and publish messages
                foreach (var queue in queues)
                {
                    await channel.QueueDeclareAsync(queue.Value, durable: false, exclusive: false, autoDelete: false, arguments: null);
                    await channel.QueueBindAsync(queue.Value, exchangeName, queue.Key);
                    await channel.BasicPublishAsync(exchange: exchangeName, routingKey: queue.Key, body: body);
                }

                _logger.Info($"[RabbitMQSender] Successfully published message to exchange '{exchangeName}' with {queues.Count} queues");
            }
            catch (Exception ex)
            {
                _logger.Error($"[RabbitMQSender] Error publishing message to exchange '{exchangeName}'", ex);
                throw;
            }
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Ensures that a valid RabbitMQ connection exists.
        /// Uses a semaphore to prevent race conditions during connection creation.
        /// </summary>
        /// <returns>A task representing the connection validation operation</returns>
        private async Task EnsureConnectionAsync()
        {
            // Quick check without locking if connection is already valid
            if (_connection != null && _connection.IsOpen)
                return;

            // Use semaphore to ensure thread-safe connection creation
            await _connectionSemaphore.WaitAsync();
            try
            {
                // Double-check pattern: verify connection is still needed after acquiring lock
                if (_connection != null && _connection.IsOpen)
                    return;

                await CreateConnectionAsync();
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// Creates a new RabbitMQ connection with recovery settings.
        /// Disposes any existing connection before creating a new one.
        /// </summary>
        /// <returns>A task representing the connection creation operation</returns>
        /// <exception cref="Exception">Rethrows any connection creation exceptions</exception>
        private async Task CreateConnectionAsync()
        {
            try
            {
                _logger.Debug($"[RabbitMQSender] Creating connection to {_hostName}");

                // Dispose existing connection if any
                _connection?.Dispose();

                // Configure connection factory with recovery settings
                var factory = new ConnectionFactory
                {
                    HostName = _hostName,
                    UserName = _username,
                    Password = _password,
                    AutomaticRecoveryEnabled = true,        // Enable automatic recovery
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10), // Recovery interval
                    RequestedHeartbeat = TimeSpan.FromSeconds(60)       // Heartbeat interval
                };

                _connection = await factory.CreateConnectionAsync();
                _logger.Info($"[RabbitMQSender] Connection established to {_hostName}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[RabbitMQSender] Failed to create connection to {_hostName}", ex);
                throw;
            }
        }

        #endregion

        #region Message Serialization

        /// <summary>
        /// Serializes a message object to UTF-8 encoded JSON bytes.
        /// </summary>
        /// <param name="message">The message object to serialize</param>
        /// <returns>UTF-8 encoded JSON bytes representing the message</returns>
        private static byte[] SerializeMessage(object message)
        {
            var json = JsonConvert.SerializeObject(message);
            return Encoding.UTF8.GetBytes(json);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _logger.Debug("[RabbitMQSender] Disposing resources");

                    // Dispose managed resources
                    _connection?.Dispose();
                    _connectionSemaphore?.Dispose();

                    _logger.Info("[RabbitMQSender] Resources disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger.Error("[RabbitMQSender] Error during disposal", ex);
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        #endregion
    }
}
