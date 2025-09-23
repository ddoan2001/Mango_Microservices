using log4net;
using Mango.Message.RabbitMQ.Consumer.Base;
using Mango.Message.RabbitMQ.Models;
using Mango.Services.OrderAPI.Models.Dto.Payment;
using Mango.Services.OrderAPI.Service.IService;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Mango.Services.OrderAPI.Messaging.RabbitMQ
{
    /// <summary>
    /// RabbitMQ consumer for processing payment-related order updates.
    /// Handles order status updates when payment events occur in the system.
    /// </summary>
    public class RabbitMQOrderConsumer : RabbitMQBaseConsumer
    {
        #region Private Fields

        /// <summary>
        /// Service factory for creating scoped services
        /// </summary>
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Application configuration
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Logger instance for this consumer
        /// </summary>
        private static readonly ILog _logger = LogManager.GetLogger(typeof(RabbitMQOrderConsumer));

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the RabbitMQOrderConsumer class.
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="scopeFactory">Service scope factory for dependency resolution</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        /// <exception cref="InvalidOperationException">Thrown when exchange configuration is missing</exception>
        public RabbitMQOrderConsumer(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            IOptions<RabbitMQConnectionOptions> rabbitMQOptions) : base(rabbitMQOptions.Value)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            _logger.Info("[RabbitMQOrderConsumer] Initialized for payment-based order status updates");
        }

        #endregion

        #region Exchange Configuration

        /// <summary>
        /// Gets the exchange name for payment events.
        /// </summary>
        protected override string? ExchangeName =>
            _configuration["TopicAndQueueNames:PaymentCreatedTopic"] ??
            throw new InvalidOperationException("Configuration 'TopicAndQueueNames:PaymentCreatedTopic' is required but not found");

        /// <summary>
        /// Gets the queue configuration for order-specific payment events.
        /// Maps the routing key to the queue name for order processing.
        /// </summary>
        protected override KeyValuePair<string, string> Queue
        {
            get
            {
                var routingKey = _configuration["TopicAndQueueNames:PaymentCreatedSub_Order_Key"] ??
                    throw new InvalidOperationException("Configuration 'TopicAndQueueNames:PaymentCreatedSub_Order_Key' is required but not found");

                var queueName = _configuration["TopicAndQueueNames:PaymentCreatedSub_Order_Value"] ??
                    throw new InvalidOperationException("Configuration 'TopicAndQueueNames:PaymentCreatedSub_Order_Value' is required but not found");

                return new KeyValuePair<string, string>(routingKey, queueName);
            }
        }

        #endregion

        #region Dead Letter Queue Configuration

        /// <summary>
        /// Enable Dead Letter Queue for failed order status updates.
        /// Order updates are critical for business operations, so DLQ is essential.
        /// </summary>
        protected override bool EnableDeadLetterQueue => false;

        /// <summary>
        /// Set maximum retry attempts for order status updates.
        /// Order processing is critical, so more retry attempts are warranted.
        /// </summary>
        protected override int MaxRetryAttempts => 5;

        /// <summary>
        /// Set retry delay for order status updates.
        /// Longer delay for database operations which might need time to recover.
        /// </summary>
        protected override int RetryDelayMilliseconds => 10000;

        #endregion

        #region Message Handling

        /// <summary>
        /// Handles incoming payment messages and updates corresponding order status.
        /// </summary>
        /// <param name="body">The message body containing payment information</param>
        /// <returns>A task representing the asynchronous message handling operation</returns>
        protected override async Task HandleMessageAsync(string body)
        {
            try
            {
                _logger.Debug($"[RabbitMQOrderConsumer] Processing payment message for order update: {body?.Substring(0, Math.Min(200, body?.Length ?? 0))}...");

                // Validate message body
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.Warn("[RabbitMQOrderConsumer] Received empty or null message body - skipping processing");
                    return;
                }

                // Deserialize the payment message
                PaymentQueueDto? paymentMessage;
                try
                {
                    paymentMessage = JsonConvert.DeserializeObject<PaymentQueueDto>(body);
                }
                catch (JsonException ex)
                {
                    _logger.Error($"[RabbitMQOrderConsumer] Failed to deserialize payment message: {ex.Message}");
                    throw new InvalidOperationException($"Invalid payment message format: {ex.Message}", ex);
                }

                // Validate deserialized payment data
                if (paymentMessage == null)
                {
                    _logger.Warn("[RabbitMQOrderConsumer] Deserialized payment message is null - skipping processing");
                    return;
                }

                // Additional validation for payment data
                if (string.IsNullOrWhiteSpace(paymentMessage.PaymentIntentId))
                {
                    _logger.Warn($"[RabbitMQOrderConsumer] Invalid PaymentIntentId in payment message - skipping processing");
                    return;
                }

                // Process order update using scoped service
                using var scope = _scopeFactory.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                _logger.Info($"[RabbitMQOrderConsumer] Updating order status for PaymentIntentId: {paymentMessage.PaymentIntentId}, Status: {paymentMessage.Status}");

                await orderService.UpdateOrderStatus(paymentMessage);

                _logger.Info($"[RabbitMQOrderConsumer] Successfully updated order status for PaymentIntentId: {paymentMessage.PaymentIntentId}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[RabbitMQOrderConsumer] Error processing payment message for order update: {ex.Message}", ex);

                // Re-throw to allow the base consumer to handle the error appropriately
                // In production, you might want to implement dead letter queue handling here
                throw;
            }
        }

        #endregion
    }
}
