using Azure.Messaging.ServiceBus;
using log4net;
using Mango.Services.RewardAPI.Message;
using Mango.Services.RewardAPI.Service.IService;
using Newtonsoft.Json;
using System.Text;

namespace Mango.Services.RewardAPI.Messaging.Azure
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string serviceBusConnectionString;
        private readonly string paymentCreatedTopic;
        private readonly string paymentCreatedRewardSubscription;
        private readonly IConfiguration _configuration;
        private readonly IRewardService _rewardService;
        private readonly ServiceBusProcessor _rewardProcessor;

        /// <summary>
        /// Constructor for AzureServiceBusConsumer that initializes the service bus connection and queue names.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="emailService"></param>
        public AzureServiceBusConsumer(IConfiguration configuration, IRewardService rewardService)
        {
            _rewardService = rewardService;
            _configuration = configuration;

            serviceBusConnectionString = _configuration.GetConnectionString("ServiceBus") ?? "";

            paymentCreatedTopic = _configuration["TopicAndQueueNames:PaymentCreatedTopic"] ?? "";
            paymentCreatedRewardSubscription = _configuration["TopicAndQueueNames:PaymentCreated_Reward_Subscription"] ?? "";

            var client = new ServiceBusClient(serviceBusConnectionString);
            _rewardProcessor = client.CreateProcessor(paymentCreatedTopic, paymentCreatedRewardSubscription);
        }

        /// <summary>
        /// Starts the Azure Service Bus Consumer for Reward Queues.
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            _logger.Info("Starting Azure Service Bus Consumer for Reward Queue...");

            // 1. Payment Reward
            _logger.Info($"Starting Reward Processor for queue: {paymentCreatedTopic}");
            _rewardProcessor.ProcessMessageAsync += OnNewPaymentRewardsRequestReceived;
            _rewardProcessor.ProcessErrorAsync += ErrorHandler;
            await _rewardProcessor.StartProcessingAsync();

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
        private async Task OnNewPaymentRewardsRequestReceived(ProcessMessageEventArgs args)
        {
            // This is where you will receive message
            _logger.Info("OnRewardRequestReceived triggered.");
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            PaymentQueueDto objMessage = JsonConvert.DeserializeObject<PaymentQueueDto>(body);
            try
            {
                //TODO - try to log email
                await _rewardService.UpdateRewards(objMessage);
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                _logger.Error("Error occurred while processing Reward request:", ex);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Stops the Azure Service Bus Consumer and disposes of the processors.
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            _logger.Info("Stopping Azure Service Bus Consumer for Reward Queue...");

            // 1. Email Cart
            await _rewardProcessor.StopProcessingAsync();
            await _rewardProcessor.DisposeAsync();

            _logger.Info("Azure Service Bus Consumer stopped successfully.");
        }
    }
}
