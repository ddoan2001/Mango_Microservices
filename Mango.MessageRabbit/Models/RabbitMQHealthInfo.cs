using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mango.Message.RabbitMQ.Models
{
    /// <summary>
    /// Represents detailed health information for a RabbitMQ consumer.
    /// Used by health monitoring systems to assess consumer status and performance.
    /// </summary>
    public class RabbitMQHealthInfo
    {
        /// <summary>
        /// Current health status of the consumer (Healthy, Degraded, or Unhealthy).
        /// </summary>
        public HealthStatus HealthStatus { get; set; }

        /// <summary>
        /// Name of the consumer class for identification purposes.
        /// </summary>
        public string ConsumerName { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the RabbitMQ connection is currently open and active.
        /// </summary>
        public bool IsConnectionOpen { get; set; }

        /// <summary>
        /// Indicates whether the RabbitMQ channel is currently open and active.
        /// </summary>
        public bool IsChannelOpen { get; set; }

        /// <summary>
        /// Name of the queue being consumed from.
        /// </summary>
        public string? QueueName { get; set; }

        /// <summary>
        /// Name of the exchange being consumed from (for exchange-based consumers).
        /// </summary>
        public string? ExchangeName { get; set; }

        /// <summary>
        /// Timestamp of the last successful message processing.
        /// Null if no messages have been processed successfully yet.
        /// </summary>
        public DateTime? LastSuccessfulProcessing { get; set; }

        /// <summary>
        /// Timestamp of the last health check performed.
        /// </summary>
        public DateTime LastHealthCheck { get; set; }

        /// <summary>
        /// Total number of messages processed successfully since consumer startup.
        /// </summary>
        public long SuccessfulMessageCount { get; set; }

        /// <summary>
        /// Total number of message processing failures since consumer startup.
        /// </summary>
        public long FailedMessageCount { get; set; }

        /// <summary>
        /// Indicates whether Dead Letter Queue is enabled for this consumer.
        /// </summary>
        public bool DeadLetterQueueEnabled { get; set; }

        /// <summary>
        /// Maximum number of retry attempts before sending messages to DLQ.
        /// </summary>
        public int MaxRetryAttempts { get; set; }

        /// <summary>
        /// Base retry delay in milliseconds used for exponential backoff.
        /// </summary>
        public int RetryDelayMilliseconds { get; set; }

        /// <summary>
        /// Calculated message failure rate as a percentage.
        /// </summary>
        public double FailureRatePercentage 
        { 
            get 
            { 
                var total = SuccessfulMessageCount + FailedMessageCount;
                return total > 0 ? (double)FailedMessageCount / total * 100 : 0;
            } 
        }

        /// <summary>
        /// Indicates whether the consumer has processed messages recently (within the last 5 minutes).
        /// </summary>
        public bool IsActivelyProcessing 
        { 
            get 
            { 
                return LastSuccessfulProcessing.HasValue && 
                       LastSuccessfulProcessing.Value > DateTime.UtcNow.AddMinutes(-5);
            } 
        }

        /// <summary>
        /// Provides a human-readable summary of the consumer's health status.
        /// </summary>
        public string HealthSummary
        {
            get
            {
                var summary = new List<string>();
                
                if (!IsConnectionOpen) summary.Add("Connection closed");
                if (!IsChannelOpen) summary.Add("Channel closed");
                if (FailureRatePercentage > 10) summary.Add($"High failure rate: {FailureRatePercentage:F1}%");
                if (!IsActivelyProcessing && SuccessfulMessageCount == 0) summary.Add("No messages processed");
                
                return summary.Any() ? string.Join(", ", summary) : "All systems operational";
            }
        }
    }
}
