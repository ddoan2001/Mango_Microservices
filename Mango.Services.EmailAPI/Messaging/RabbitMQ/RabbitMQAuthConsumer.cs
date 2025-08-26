using log4net;
using Mango.Message.RabbitMQ.Consumer.Base;
using Mango.Services.EmailAPI.Service.IService;
using Newtonsoft.Json;

namespace Mango.Services.EmailAPI.Messaging.RabbitMQ
{
    /// <summary>
    /// RabbitMQ consumer for processing user registration messages.
    /// Handles email notifications when users register in the system.
    /// </summary>
    public class RabbitMQAuthConsumer : RabbitMQBaseConsumer
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
        private static readonly ILog _logger = LogManager.GetLogger(typeof(RabbitMQAuthConsumer));

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the RabbitMQAuthConsumer class.
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="scopeFactory">Service scope factory for dependency resolution</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        /// <exception cref="InvalidOperationException">Thrown when queue configuration is missing</exception>
        public RabbitMQAuthConsumer(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            _logger.Info("[RabbitMQAuthConsumer] Initialized for user registration email processing");
        }

        #endregion

        #region Queue Configuration

        /// <summary>
        /// Gets the queue name for user registration messages.
        /// </summary>
        protected override string? QueueName =>
            _configuration["TopicAndQueueNames:RegisterUserQueue"] ??
            throw new InvalidOperationException("Configuration 'TopicAndQueueNames:RegisterUserQueue' is required but not found");

        #endregion

        #region Dead Letter Queue Configuration

        /// <summary>
        /// Enable Dead Letter Queue for failed registration email processing.
        /// Failed messages will be sent to DLQ after retry attempts are exhausted.
        /// </summary>
        protected override bool EnableDeadLetterQueue => false;

        /// <summary>
        /// Set maximum retry attempts for registration email processing.
        /// Email failures are typically due to network issues or service unavailability.
        /// </summary>
        protected override int MaxRetryAttempts => 2;

        /// <summary>
        /// Set retry delay for registration email processing.
        /// Shorter delay for email processing as they're typically quick operations.
        /// </summary>
        protected override int RetryDelayMilliseconds => 3000;

        #endregion

        #region Message Handling

        /// <summary>
        /// Handles incoming user registration messages and sends welcome emails.
        /// </summary>
        /// <param name="body">The message body containing the user email address</param>
        /// <returns>A task representing the asynchronous message handling operation</returns>
        protected override async Task HandleMessageAsync(string body)
        {
            try
            {
                _logger.Debug($"[RabbitMQAuthConsumer] Processing user registration message: {body?.Substring(0, Math.Min(100, body?.Length ?? 0))}...");

                // Validate message body
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.Warn("[RabbitMQAuthConsumer] Received empty or null message body - skipping processing");
                    return;
                }

                // Deserialize the email address
                string? userEmail;
                try
                {
                    userEmail = JsonConvert.DeserializeObject<string>(body);
                }
                catch (JsonException ex)
                {
                    _logger.Error($"[RabbitMQAuthConsumer] Failed to deserialize message: {ex.Message}");
                    throw new InvalidOperationException($"Invalid message format: {ex.Message}", ex);
                }

                // Validate deserialized email
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    _logger.Warn("[RabbitMQAuthConsumer] Deserialized email is empty or null - skipping processing");
                    return;
                }

                // Process email using scoped service
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                _logger.Info($"[RabbitMQAuthConsumer] Sending welcome email to: {userEmail}");

                await emailService.EmailRegisterUserAndLog(userEmail);

                _logger.Info($"[RabbitMQAuthConsumer] Successfully processed registration email for: {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[RabbitMQAuthConsumer] Error processing registration message: {ex.Message}", ex);

                // Re-throw to allow the base consumer to handle the error appropriately
                // In production, you might want to implement dead letter queue handling here
                throw;
            }
        }

        #endregion
    }
}
