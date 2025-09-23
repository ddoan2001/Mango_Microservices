using log4net;
using Mango.Message.RabbitMQ.Consumer.Base;
using Mango.Message.RabbitMQ.Models;
using Mango.Services.RewardAPI.Message;
using Mango.Services.RewardAPI.Service.IService;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Mango.Services.RewardAPI.Messaging.RabbitMQ
{
    /// <summary>
    /// RabbitMQ consumer for processing payment-related reward calculations.
    /// Handles reward point calculations when payment events occur in the system.
    /// </summary>
    public class RabbitMQRewardConsumer : RabbitMQBaseConsumer
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
        private static readonly ILog _logger = LogManager.GetLogger(typeof(RabbitMQRewardConsumer));

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the RabbitMQRewardConsumer class.
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="scopeFactory">Service scope factory for dependency resolution</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        /// <exception cref="InvalidOperationException">Thrown when exchange configuration is missing</exception>
        public RabbitMQRewardConsumer(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            IOptions<RabbitMQConnectionOptions> rabbitMQOptions) : base(rabbitMQOptions.Value)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            _logger.Info("[RabbitMQRewardConsumer] Initialized for payment-based reward calculations");
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
        /// Gets the queue configuration for reward-specific payment events.
        /// Maps the routing key to the queue name for reward processing.
        /// </summary>
        protected override KeyValuePair<string, string> Queue
        {
            get
            {
                var routingKey = _configuration["TopicAndQueueNames:PaymentCreatedSub_Reward_Key"] ??
                    throw new InvalidOperationException("Configuration 'TopicAndQueueNames:PaymentCreatedSub_Reward_Key' is required but not found");

                var queueName = _configuration["TopicAndQueueNames:PaymentCreatedSub_Reward_Value"] ??
                    throw new InvalidOperationException("Configuration 'TopicAndQueueNames:PaymentCreatedSub_Reward_Value' is required but not found");

                return new KeyValuePair<string, string>(routingKey, queueName);
            }
        }

        #endregion

        #region Dead Letter Queue Configuration

        /// <summary>
        /// Enable Dead Letter Queue for failed reward calculations.
        /// Reward processing failures should be preserved for manual review.
        /// </summary>
        protected override bool EnableDeadLetterQueue => false;

        /// <summary>
        /// Set maximum retry attempts for reward calculations.
        /// Reward calculations involve database operations, so moderate retry attempts.
        /// </summary>
        protected override int MaxRetryAttempts => 3;

        /// <summary>
        /// Set retry delay for reward calculations.
        /// Standard delay for database-related operations.
        /// </summary>
        protected override int RetryDelayMilliseconds => 5000;

        #endregion

        #region Message Handling

        /// <summary>
        /// Handles incoming payment messages and calculates corresponding reward points.
        /// </summary>
        /// <param name="body">The message body containing payment information</param>
        /// <returns>A task representing the asynchronous message handling operation</returns>
        protected override async Task HandleMessageAsync(string body)
        {
            try
            {
                _logger.Debug($"[RabbitMQRewardConsumer] Processing payment message for reward calculation: {body?.Substring(0, Math.Min(200, body?.Length ?? 0))}...");

                // Validate message body
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.Warn("[RabbitMQRewardConsumer] Received empty or null message body - skipping processing");
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
                    _logger.Error($"[RabbitMQRewardConsumer] Failed to deserialize payment message: {ex.Message}");
                    throw new InvalidOperationException($"Invalid payment message format: {ex.Message}", ex);
                }

                // Validate deserialized payment data
                if (paymentMessage == null)
                {
                    _logger.Warn("[RabbitMQRewardConsumer] Deserialized payment message is null - skipping processing");
                    return;
                }

                // Additional validation for payment data
                if (paymentMessage.Total <= 0)
                {
                    _logger.Warn($"[RabbitMQRewardConsumer] Invalid payment total in message: {paymentMessage.Total} - skipping reward calculation");
                    return;
                }

                if (string.IsNullOrWhiteSpace(paymentMessage.PaymentIntentId))
                {
                    _logger.Warn("[RabbitMQRewardConsumer] Invalid PaymentIntentId in payment message - skipping processing");
                    return;
                }

                // Process reward calculation using scoped service
                using var scope = _scopeFactory.CreateScope();
                var rewardService = scope.ServiceProvider.GetRequiredService<IRewardService>();

                _logger.Info($"[RabbitMQRewardConsumer] Calculating rewards for PaymentIntentId: {paymentMessage.PaymentIntentId}, Total: {paymentMessage.Total:C}");

                await rewardService.UpdateRewards(paymentMessage);

                _logger.Info($"[RabbitMQRewardConsumer] Successfully calculated rewards for PaymentIntentId: {paymentMessage.PaymentIntentId}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[RabbitMQRewardConsumer] Error processing payment message for reward calculation: {ex.Message}", ex);

                // Re-throw to allow the base consumer to handle the error appropriately
                // In production, you might want to implement dead letter queue handling here
                throw;
            }
        }

        #endregion
    }
}
