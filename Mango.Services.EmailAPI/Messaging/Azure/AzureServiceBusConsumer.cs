using Azure.Messaging.ServiceBus;
using log4net;
using Mango.Services.EmailAPI.Models.Dto.Cart;
using Mango.Services.EmailAPI.Service.IService;
using Newtonsoft.Json;
using System.Text;

namespace Mango.Services.EmailAPI.Messaging.Azure
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string serviceBusConnectionString;
        private readonly string emailCartQueue;
        private readonly string emailRegisterUserQueue;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ServiceBusProcessor _emailCartProcessor;
        private readonly ServiceBusProcessor _emailRegisterUserProcessor;

        /// <summary>
        /// Constructor for AzureServiceBusConsumer that initializes the service bus connection and queue names.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="emailService"></param>
        public AzureServiceBusConsumer(IConfiguration configuration, IEmailService emailService)
        {
            _emailService = emailService;
            _configuration = configuration;

            serviceBusConnectionString = _configuration.GetConnectionString("ServiceBus") ?? "";

            emailCartQueue = _configuration["TopicAndQueueNames:EmailShoppingCartQueue"] ?? "";
            emailRegisterUserQueue = _configuration["TopicAndQueueNames:RegisterUserQueue"] ?? "";

            var client = new ServiceBusClient(serviceBusConnectionString);
            _emailCartProcessor = client.CreateProcessor(emailCartQueue);
            _emailRegisterUserProcessor = client.CreateProcessor(emailRegisterUserQueue);
        }

        /// <summary>
        /// Starts the Azure Service Bus Consumer for Email Queues.
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            _logger.Info("Starting Azure Service Bus Consumer for Email Queue...");

            // 1. Email Cart
            _logger.Info($"Starting Email Cart Processor for queue: {emailCartQueue}");
            _emailCartProcessor.ProcessMessageAsync += OnEmailCartRequestReceived;
            _emailCartProcessor.ProcessErrorAsync += ErrorHandler;
            await _emailCartProcessor.StartProcessingAsync();

            // 2. Email Register User
            _logger.Info($"Starting Email Register User Processor for queue: {emailRegisterUserQueue}");
            _emailRegisterUserProcessor.ProcessMessageAsync += OnEmailRegisterUserRequestReceived;
            _emailRegisterUserProcessor.ProcessErrorAsync += ErrorHandler;
            await _emailRegisterUserProcessor.StartProcessingAsync();

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
        /// This method is triggered when a message is received from the Email Cart queue.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task OnEmailCartRequestReceived(ProcessMessageEventArgs args)
        {
            // This is where you will receive message
            _logger.Info("OnEmailCartRequestReceived triggered.");
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            CartHeaderDto objMessage = JsonConvert.DeserializeObject<CartHeaderDto>(body);
            try
            {
                //TODO - try to log email
                await _emailService.EmailCartAndLog(objMessage);
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                _logger.Error("Error occurred while processing email cart request:", ex);
                throw new Exception(ex.Message);
            }
        }


        /// <summary>
        /// This method is triggered when a message is received from the Email Register User queue.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task OnEmailRegisterUserRequestReceived(ProcessMessageEventArgs args)
        {
            // This is where you will receive message
            _logger.Info("OnEmailRegisterUserRequestReceived triggered.");
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            string email = JsonConvert.DeserializeObject<string>(body);
            try
            {
                //TODO - try to log email
                await _emailService.EmailRegisterUserAndLog(email);
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                _logger.Error("Error occurred while processing email cart request:", ex);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Stops the Azure Service Bus Consumer and disposes of the processors.
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            _logger.Info("Stopping Azure Service Bus Consumer for Email Queue...");

            // 1. Email Cart
            await _emailCartProcessor.StopProcessingAsync();
            await _emailCartProcessor.DisposeAsync();

            // 2. Email Register User
            await _emailRegisterUserProcessor.StopProcessingAsync();
            await _emailRegisterUserProcessor.DisposeAsync();

            _logger.Info("Azure Service Bus Consumer stopped successfully.");
        }
    }
}
