using Azure.Messaging.ServiceBus;
using log4net;
using Mango.Services.OrderAPI.Models.Dto.Payment;
using Mango.Services.OrderAPI.Service.IService;
using Newtonsoft.Json;
using System.Text;

namespace Mango.Services.OrderAPI.Messaging.Azure
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string serviceBusConnectionString;
        private readonly string paymentCreatedTopic;
        private readonly string paymentCreatedOrderSubscription;
        private readonly IConfiguration _configuration;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ServiceBusProcessor _paymentProcessor;

        /// <summary>
        /// Constructor for AzureServiceBusConsumer that initializes the service bus connection and queue names.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="emailService"></param>
        public AzureServiceBusConsumer(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;

            serviceBusConnectionString = _configuration.GetConnectionString("ServiceBus") ?? "";

            paymentCreatedTopic = _configuration["TopicAndQueueNames:PaymentCreatedTopic"] ?? "";
            paymentCreatedOrderSubscription = _configuration["TopicAndQueueNames:PaymentCreated_Order_Subscription"] ?? "";

            var client = new ServiceBusClient(serviceBusConnectionString);
            _paymentProcessor = client.CreateProcessor(paymentCreatedTopic, paymentCreatedOrderSubscription);
        }

        /// <summary>
        /// Starts the Azure Service Bus Consumer for Email Queues.
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            _logger.Info("Starting Azure Service Bus Consumer for Order Queue...");

            // 1. Payment
            _logger.Info($"Starting Order Payment Processor for queue: {paymentCreatedTopic}");
            _paymentProcessor.ProcessMessageAsync += OnPaymentRequestReceived;
            _paymentProcessor.ProcessErrorAsync += ErrorHandler;
            await _paymentProcessor.StartProcessingAsync();

            _logger.Info("Azure Service Bus Consumer started successfully.");
        }

        /// <summary>
        /// Handles errors that occur during message processing in the Azure Service Bus.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.Error("Error occurred in the message processing", args.Exception);
            return Task.CompletedTask;
        }


        /// <summary>
        /// This method is triggered when a message is received from the Payment queue.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task OnPaymentRequestReceived(ProcessMessageEventArgs args)
        {
            // This is where you will receive message
            _logger.Info("OnPaymentRequestReceived triggered.");
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            PaymentQueueDto objMessage = JsonConvert.DeserializeObject<PaymentQueueDto>(body);
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    await orderService.UpdateOrderStatus(objMessage);
                }

                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                _logger.Error("Error occurred while processing payment request:", ex);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Stops the Azure Service Bus Consumer and disposes of the processors.
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            _logger.Info("Stopping Azure Service Bus Consumer for Order Queue...");

            // 1. Payment
            await _paymentProcessor.StopProcessingAsync();
            await _paymentProcessor.DisposeAsync();

            _logger.Info("Azure Service Bus Consumer stopped successfully.");
        }
    }
}
