using Microsoft.AspNetCore.Mvc;
using Mango.Message.RabbitMQ.Consumer.Base;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mango.Services.EmailAPI.Controllers
{
    /// <summary>
    /// Health monitoring controller for EmailAPI service.
    /// Provides detailed health information including RabbitMQ consumer status.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IEnumerable<IHostedService> _hostedServices;
        private readonly HealthCheckService _healthCheckService;

        /// <summary>
        /// Initializes a new instance of the HealthController.
        /// </summary>
        /// <param name="hostedServices">Collection of hosted services including RabbitMQ consumers</param>
        /// <param name="healthCheckService">ASP.NET Core health check service</param>
        public HealthController(IEnumerable<IHostedService> hostedServices, HealthCheckService healthCheckService)
        {
            _hostedServices = hostedServices;
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// Gets overall health status of the EmailAPI service.
        /// </summary>
        /// <returns>Health status with basic information</returns>
        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var response = new
            {
                Status = healthReport.Status.ToString(),
                TotalDuration = healthReport.TotalDuration.TotalMilliseconds,
                Entries = healthReport.Entries.Select(entry => new
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    Description = entry.Value.Description,
                    Duration = entry.Value.Duration.TotalMilliseconds
                })
            };

            return Ok(response);
        }

        /// <summary>
        /// Gets detailed health information for RabbitMQ consumers.
        /// </summary>
        /// <returns>Detailed RabbitMQ consumer health status</returns>
        [HttpGet("rabbitmq")]
        public IActionResult GetRabbitMQHealth()
        {
            var consumers = _hostedServices.OfType<RabbitMQBaseConsumer>().ToList();
            
            if (!consumers.Any())
            {
                return Ok(new
                {
                    Status = "No RabbitMQ consumers found",
                    Consumers = new object[0]
                });
            }

            var consumerHealthInfo = consumers.Select(consumer =>
            {
                var healthInfo = consumer.GetHealthInfo();
                return new
                {
                    ConsumerName = healthInfo.ConsumerName,
                    Status = healthInfo.HealthStatus.ToString(),
                    ConnectionOpen = healthInfo.IsConnectionOpen,
                    ChannelOpen = healthInfo.IsChannelOpen,
                    QueueName = healthInfo.QueueName,
                    ExchangeName = healthInfo.ExchangeName,
                    SuccessfulMessages = healthInfo.SuccessfulMessageCount,
                    FailedMessages = healthInfo.FailedMessageCount,
                    FailureRate = $"{healthInfo.FailureRatePercentage:F2}%",
                    LastSuccessfulProcessing = healthInfo.LastSuccessfulProcessing,
                    IsActivelyProcessing = healthInfo.IsActivelyProcessing,
                    DeadLetterQueueEnabled = healthInfo.DeadLetterQueueEnabled,
                    MaxRetryAttempts = healthInfo.MaxRetryAttempts,
                    RetryDelayMilliseconds = healthInfo.RetryDelayMilliseconds,
                    HealthSummary = healthInfo.HealthSummary,
                    LastHealthCheck = healthInfo.LastHealthCheck
                };
            }).ToList();

            var overallStatus = consumers.All(c => c.HealthStatus == HealthStatus.Healthy) 
                ? HealthStatus.Healthy
                : consumers.Any(c => c.HealthStatus == HealthStatus.Unhealthy) 
                    ? HealthStatus.Unhealthy 
                    : HealthStatus.Degraded;

            return Ok(new
            {
                OverallStatus = overallStatus.ToString(),
                TotalConsumers = consumers.Count,
                HealthyConsumers = consumers.Count(c => c.HealthStatus == HealthStatus.Healthy),
                DegradedConsumers = consumers.Count(c => c.HealthStatus == HealthStatus.Degraded),
                UnhealthyConsumers = consumers.Count(c => c.HealthStatus == HealthStatus.Unhealthy),
                Consumers = consumerHealthInfo,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Triggers a manual health check for all RabbitMQ consumers.
        /// </summary>
        /// <returns>Updated health status after the check</returns>
        [HttpPost("rabbitmq/check")]
        public IActionResult CheckRabbitMQHealth()
        {
            var consumers = _hostedServices.OfType<RabbitMQBaseConsumer>().ToList();
            
            if (!consumers.Any())
            {
                return Ok(new { Message = "No RabbitMQ consumers found" });
            }

            var healthResults = consumers.Select(consumer =>
            {
                var status = consumer.CheckHealth();
                var healthInfo = consumer.GetHealthInfo();
                
                return new
                {
                    ConsumerName = healthInfo.ConsumerName,
                    Status = status.ToString(),
                    HealthSummary = healthInfo.HealthSummary
                };
            }).ToList();

            return Ok(new
            {
                Message = "Health check completed",
                Results = healthResults,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
