namespace Mango.Message.RabbitMQ.Sender.Interface
{
    /// <summary>
    /// Interface for RabbitMQ message publishing operations.
    /// Provides methods for sending messages using both simple queue and exchange-based patterns.
    /// </summary>
    public interface IRabbitMQSender : IDisposable
    {
        /// <summary>
        /// Publishes a message to a simple queue using the default exchange.
        /// </summary>
        /// <param name="message">The message object to publish</param>
        /// <param name="queueName">The name of the target queue</param>
        /// <returns>A task representing the asynchronous publish operation</returns>
        Task PublishMessage(object message, string queueName);

        /// <summary>
        /// Publishes a message to multiple queues through a direct exchange with routing keys.
        /// </summary>
        /// <param name="message">The message object to publish</param>
        /// <param name="exchangeName">The name of the exchange to publish to</param>
        /// <param name="queues">Dictionary mapping routing keys to queue names</param>
        /// <returns>A task representing the asynchronous publish operation</returns>
        Task PublishMessage(object message, string exchangeName, Dictionary<string, string> queues);
    }
}
