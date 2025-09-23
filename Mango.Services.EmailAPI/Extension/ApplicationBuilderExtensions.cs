using Mango.Services.EmailAPI.Messaging.Azure;

namespace Mango.Services.EmailAPI.Extension
{
    public static class ApplicationBuilderExtensions
    {
        private static IAzureServiceBusConsumer ServiceBusConsumer { get; set; }

        // This extension method is used to register the Azure Service Bus consumer in the application pipeline.
        public static IApplicationBuilder UseAzureServiceBusConsumer(this IApplicationBuilder app)
        {
            // Retrieve the IAzureServiceBusConsumer service from the application services
            // and the IHostApplicationLifetime service to manage application lifecycle events.
            ServiceBusConsumer = app.ApplicationServices.GetService<IAzureServiceBusConsumer>();
            var hostApplicationLife = app.ApplicationServices.GetService<IHostApplicationLifetime>();

            // Register event handlers for application start and stop events.
            hostApplicationLife.ApplicationStarted.Register(OnStart);
            hostApplicationLife.ApplicationStopping.Register(OnStop);

            return app;
        }

        /// <summary>
        /// Handles the application stop event by stopping the Service Bus consumer.
        /// </summary>
        private static void OnStop()
        {
            ServiceBusConsumer.Stop();
        }

        /// <summary>
        /// Handles the application start event by starting the Service Bus consumer.
        /// </summary>
        private static void OnStart()
        {
            ServiceBusConsumer.Start();
        }
    }
}
