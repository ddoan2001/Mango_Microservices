using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Mango.Message.RabbitMQ.Consumer.Base;

namespace Mango.Message.RabbitMQ.HealthChecks
{
    /// <summary>
    /// Health check implementation for RabbitMQ consumers.
    /// Integrates with ASP.NET Core health check system to provide consumer health monitoring.
    /// </summary>
    public class RabbitMQConsumerHealthCheck : IHealthCheck
    {
        private readonly IEnumerable<RabbitMQBaseConsumer> _consumers;
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of the RabbitMQConsumerHealthCheck.
        /// </summary>
        /// <param name="consumers">Collection of RabbitMQ consumers to monitor</param>
        /// <param name="name">Name for this health check instance</param>
        public RabbitMQConsumerHealthCheck(IEnumerable<RabbitMQBaseConsumer> consumers, string name = "RabbitMQ Consumers")
        {
            _consumers = consumers ?? throw new ArgumentNullException(nameof(consumers));
            _name = name;
        }

        /// <summary>
        /// Performs health check on all registered RabbitMQ consumers.
        /// </summary>
        /// <param name="context">Health check context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result with detailed status information</returns>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var healthResults = new List<object>();
            var overallStatus = HealthStatus.Healthy;
            var errors = new List<string>();

            try
            {
                foreach (var consumer in _consumers)
                {
                    // Check individual consumer health
                    var consumerStatus = consumer.CheckHealth();
                    var healthInfo = consumer.GetHealthInfo();

                    // Add consumer info to results
                    healthResults.Add(new
                    {
                        Consumer = healthInfo.ConsumerName,
                        Status = consumerStatus.ToString(),
                        ConnectionOpen = healthInfo.IsConnectionOpen,
                        ChannelOpen = healthInfo.IsChannelOpen,
                        SuccessfulMessages = healthInfo.SuccessfulMessageCount,
                        FailedMessages = healthInfo.FailedMessageCount,
                        FailureRate = $"{healthInfo.FailureRatePercentage:F2}%",
                        LastSuccessfulProcessing = healthInfo.LastSuccessfulProcessing,
                        IsActivelyProcessing = healthInfo.IsActivelyProcessing,
                        HealthSummary = healthInfo.HealthSummary
                    });

                    // Determine overall health status
                    if (consumerStatus == HealthStatus.Unhealthy)
                    {
                        overallStatus = HealthStatus.Unhealthy;
                        errors.Add($"{healthInfo.ConsumerName}: {healthInfo.HealthSummary}");
                    }
                    else if (consumerStatus == HealthStatus.Degraded && overallStatus == HealthStatus.Healthy)
                    {
                        overallStatus = HealthStatus.Degraded;
                        errors.Add($"{healthInfo.ConsumerName}: {healthInfo.HealthSummary}");
                    }
                }

                // Prepare health check data
                var data = new Dictionary<string, object>
                {
                    ["consumers"] = healthResults,
                    ["totalConsumers"] = _consumers.Count(),
                    ["timestamp"] = DateTime.UtcNow
                };

                // Create appropriate health check result
                var description = errors.Any() 
                    ? $"Issues detected: {string.Join("; ", errors)}"
                    : $"All {_consumers.Count()} consumers are healthy";

                return Task.FromResult(new HealthCheckResult(overallStatus, description, data: data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}", ex, 
                    new Dictionary<string, object>
                    {
                        ["error"] = ex.Message,
                        ["timestamp"] = DateTime.UtcNow
                    }));
            }
        }
    }

    /// <summary>
    /// Extension methods for registering RabbitMQ consumer health checks.
    /// </summary>
    public static class RabbitMQHealthCheckExtensions
    {
        /// <summary>
        /// Adds RabbitMQ consumer health check to the service collection.
        /// </summary>
        /// <param name="builder">Health check builder</param>
        /// <param name="name">Health check name</param>
        /// <param name="failureStatus">Status to report when health check fails</param>
        /// <param name="tags">Tags for organizing health checks</param>
        /// <returns>Health check builder for method chaining</returns>
        public static IHealthChecksBuilder AddRabbitMQConsumers(
            this IHealthChecksBuilder builder,
            string name = "rabbitmq_consumers",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null)
        {
            return builder.Add(new HealthCheckRegistration(
                name,
                serviceProvider =>
                {
                    var consumers = serviceProvider.GetServices<RabbitMQBaseConsumer>();
                    return new RabbitMQConsumerHealthCheck(consumers, name);
                },
                failureStatus,
                tags));
        }
    }
}
