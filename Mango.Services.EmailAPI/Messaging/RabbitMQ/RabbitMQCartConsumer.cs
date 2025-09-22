using log4net;
using Mango.Message.RabbitMQ.Consumer.Base;
using Mango.Message.RabbitMQ.Models;
using Mango.Services.EmailAPI.Models.Dto.Cart;
using Mango.Services.EmailAPI.Service.IService;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Mango.Services.EmailAPI.Messaging.RabbitMQ
{
    /// <summary>
    /// RabbitMQ consumer for processing shopping cart email notifications.
    /// Handles email notifications when users perform cart-related actions.
    /// </summary>
    public class RabbitMQCartConsumer : RabbitMQBaseConsumer
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
        private static readonly ILog _logger = LogManager.GetLogger(typeof(RabbitMQCartConsumer));

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the RabbitMQCartConsumer class.
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="scopeFactory">Service scope factory for dependency resolution</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        /// <exception cref="InvalidOperationException">Thrown when queue configuration is missing</exception>
        public RabbitMQCartConsumer(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            IOptions<RabbitMQConnectionOptions> rabbitMQOptions) : base(rabbitMQOptions.Value)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            _logger.Info("[RabbitMQCartConsumer] Initialized for shopping cart email processing");
        }

        #endregion

        #region Queue Configuration

        /// <summary>
        /// Gets the queue name for shopping cart email messages.
        /// </summary>
        protected override string? QueueName =>
            _configuration["TopicAndQueueNames:EmailShoppingCartQueue"] ??
            throw new InvalidOperationException("Configuration 'TopicAndQueueNames:EmailShoppingCartQueue' is required but not found");

        #endregion

        #region Dead Letter Queue Configuration

        /// <summary>
        /// Enable Dead Letter Queue for failed cart email processing.
        /// Failed messages will be sent to DLQ after retry attempts are exhausted.
        /// </summary>
        protected override bool EnableDeadLetterQueue => false;

        /// <summary>
        /// Set maximum retry attempts for cart email processing.
        /// Cart emails are less critical, so fewer retries to avoid resource consumption.
        /// </summary>
        protected override int MaxRetryAttempts => 1;

        /// <summary>
        /// Set retry delay for cart email processing.
        /// Shorter delay for email processing as they're typically quick operations.
        /// </summary>
        protected override int RetryDelayMilliseconds => 2000;

        #endregion

        #region Message Handling

        /// <summary>
        /// Handles incoming shopping cart messages and sends cart-related emails.
        /// </summary>
        /// <param name="body">The message body containing the cart header information</param>
        /// <returns>A task representing the asynchronous message handling operation</returns>
        protected override async Task HandleMessageAsync(string body)
        {
            try
            {
                _logger.Debug($"[RabbitMQCartConsumer] Processing cart message: {body?.Substring(0, Math.Min(200, body?.Length ?? 0))}...");

                // Validate message body
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.Warn("[RabbitMQCartConsumer] Received empty or null message body - skipping processing");
                    return;
                }

                // Deserialize the cart message
                CartHeaderDto? cartMessage;
                try
                {
                    cartMessage = JsonConvert.DeserializeObject<CartHeaderDto>(body);
                }
                catch (JsonException ex)
                {
                    _logger.Error($"[RabbitMQCartConsumer] Failed to deserialize cart message: {ex.Message}");
                    throw new InvalidOperationException($"Invalid cart message format: {ex.Message}", ex);
                }

                // Validate deserialized cart data
                if (cartMessage == null)
                {
                    _logger.Warn("[RabbitMQCartConsumer] Deserialized cart message is null - skipping processing");
                    return;
                }

                // Additional validation for cart data
                if (string.IsNullOrWhiteSpace(cartMessage.Email))
                {
                    _logger.Warn($"[RabbitMQCartConsumer] Cart message missing email address - skipping processing. CartId: {cartMessage.CartHeaderId}");
                    return;
                }

                // Process email using scoped service
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                _logger.Info($"[RabbitMQCartConsumer] Sending cart email to: {cartMessage.Email}, CartId: {cartMessage.CartHeaderId}");

                await emailService.EmailCartAndLog(cartMessage);

                _logger.Info($"[RabbitMQCartConsumer] Successfully processed cart email for: {cartMessage.Email}, CartId: {cartMessage.CartHeaderId}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[RabbitMQCartConsumer] Error processing cart message: {ex.Message}", ex);

                // Re-throw to allow the base consumer to handle the error appropriately
                // In production, you might want to implement dead letter queue handling here
                throw;
            }
        }

        #endregion
    }
}
