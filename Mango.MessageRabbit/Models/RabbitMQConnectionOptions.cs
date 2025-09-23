namespace Mango.Message.RabbitMQ.Models
{
    /// <summary>
    /// Configuration model for RabbitMQ connection settings.
    /// Can be bound from appsettings.json configuration section.
    /// </summary>
    public class RabbitMQConnectionOptions
    {
        /// <summary>
        /// Configuration section name for binding from appsettings.json
        /// </summary>
        public const string SectionName = "RabbitMQ";

        /// <summary>
        /// RabbitMQ server hostname or IP address.
        /// Default: localhost
        /// </summary>
        public string HostName { get; set; } = "localhost";

        /// <summary>
        /// RabbitMQ server port.
        /// Default: 5672 (standard AMQP port)
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// Username for RabbitMQ authentication.
        /// Default: guest
        /// </summary>
        public string Username { get; set; } = "guest";

        /// <summary>
        /// Password for RabbitMQ authentication.
        /// Default: guest
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// Virtual host to use.
        /// Default: / (root virtual host)
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// Whether to use SSL/TLS for connection.
        /// Default: false
        /// </summary>
        public bool UseSsl { get; set; } = false;

        /// <summary>
        /// Connection timeout in milliseconds.
        /// Default: 30000 (30 seconds)
        /// </summary>
        public int ConnectionTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Heartbeat interval in seconds.
        /// Default: 60 seconds
        /// </summary>
        public ushort HeartbeatInterval { get; set; } = 60;
    }
}