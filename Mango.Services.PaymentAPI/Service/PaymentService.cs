using Mango.Message.RabbitMQ.Sender.Interface;
using Mango.Services.PaymentAPI.Models.Dto.Payment;
using Mango.Services.PaymentAPI.Service.IService;
using Mango.Services.PaymentAPI.Utility;
using Stripe;

namespace Mango.Services.PaymentAPI.Service
{
    public class PaymentService(
            IConfiguration config,
            IOrderService orderService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IRabbitMQSender messageBus)
            : IPaymentService
    {
        private readonly IConfiguration _config = config;
        private readonly IOrderService _orderService = orderService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IRabbitMQSender _messageBus = messageBus;

        // Topic and Queue names from appsettings.json
        #region Topic and Queue Names
        private string PaymentCreatedTopic => _configuration["TopicAndQueueNames:PaymentCreatedTopic"] ??
            throw new ArgumentNullException("PaymentCreatedTopic");
        private string PaymentCreatedSub_Order_Key => _configuration["TopicAndQueueNames:PaymentCreatedSub_Order_Key"] ??
            throw new ArgumentNullException("PaymentCreatedSub_Order_Key");
        private string PaymentCreatedSub_Order_Value => _configuration["TopicAndQueueNames:PaymentCreatedSub_Order_Value"] ??
            throw new ArgumentNullException("PaymentCreatedSub_Order_Value");
        private string PaymentCreatedSub_Reward_Key => _configuration["TopicAndQueueNames:PaymentCreatedSub_Reward_Key"] ??
            throw new ArgumentNullException("PaymentCreatedSub_Reward_Key");
        private string PaymentCreatedSub_Reward_Value => _configuration["TopicAndQueueNames:PaymentCreatedSub_Reward_Value"] ??
            throw new ArgumentNullException("PaymentCreatedSub_Reward_Value");
        #endregion

        public async Task<PaymentIntent> CreateOrUpdatePaymentIntent(PaymentDto paymentDto)
        {
            StripeConfiguration.ApiKey = _config["StripeSettings:SecretKey"];

            var service = new PaymentIntentService();

            var intent = new PaymentIntent();
            var deliveryFee = paymentDto.Total > 10000 ? 0 : 500;

            if (string.IsNullOrEmpty(paymentDto.PaymentIntentId))
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = paymentDto.Total + deliveryFee,
                    Currency = "usd",
                    PaymentMethodTypes = ["card"]
                };
                intent = await service.CreateAsync(options);
            }
            else
            {
                var options = new PaymentIntentUpdateOptions
                {
                    Amount = paymentDto.Total + deliveryFee
                };
                intent = await service.UpdateAsync(paymentDto.PaymentIntentId, options);
            }
            return intent;
        }

        public async Task<PaymentDto> CreateOrUpdatePayment(PaymentDto paymentDto)
        {
            var intent = await CreateOrUpdatePaymentIntent(paymentDto) ?? throw new Exception("Failed to create or update payment intent.");

            var existingOrder = await _orderService.GetOrder(paymentDto.OrderHeaderId) ?? throw new Exception("Failied to get existingOrder");

            // set data updating for OrderAPI
            existingOrder.OrderTotal = paymentDto.Total;
            existingOrder.DeliveryFee = paymentDto.Total > 10000 ? 0 : 500;
            existingOrder.PaymentIntentId = intent.Id;
            existingOrder.ClientSecret = intent.ClientSecret;

            var result = await _orderService.UpdateHeaderDto(existingOrder)
                ?? throw new Exception("Failed to update order header with payment intent details.");

            // set data return for current API
            paymentDto.OrderHeaderId = result.OrderHeaderId;
            paymentDto.PaymentIntentId = intent.Id;
            paymentDto.ClientSecret = intent.ClientSecret;
            paymentDto.UserId = result.UserId;

            return paymentDto;
        }

        public async Task<bool> ProcessPayment(string json)
        {
            var stripeEvent = ConstructStripeEvent(json);

            if (stripeEvent.Data.Object is not PaymentIntent intent) throw new Exception("Invalid event data");

            if (intent.Status == "succeeded") await HandlePaymentIntentSucceeded(intent);
            else await HandlePaymentIntentFailed(intent);

            return true;
        }

        private Event ConstructStripeEvent(string json)
        {
            try
            {
                return EventUtility.ConstructEvent(
                    json,
                    _httpContextAccessor?.HttpContext?.Request.Headers["Stripe-Signature"],
                    _config["StripeSettings:WhSecret"]);
            }
            catch (Exception ex)
            {
                throw new StripeException("Invalid signature", ex);
            }
        }

        private async Task HandlePaymentIntentSucceeded(PaymentIntent intent)
        {
            // need sent event to update orderStatus, clear order details, and update product stock
            Dictionary<string, string> queues = new()
                {
                    { PaymentCreatedSub_Order_Key, PaymentCreatedSub_Order_Value },
                    { PaymentCreatedSub_Reward_Key, PaymentCreatedSub_Reward_Value }
                };

            PaymentQueueDto paymentQueueDto = new()
            {
                PaymentIntentId = intent.Id,
                Status = OrderStatus.PaymentReceived,
                Total = intent.Amount
            };

            await _messageBus.PublishMessage(paymentQueueDto, PaymentCreatedTopic, queues);
        }

        private async Task HandlePaymentIntentFailed(PaymentIntent intent)
        {
            PaymentQueueDto paymentQueueDto = new()
            {
                PaymentIntentId = intent.Id,
                Status = OrderStatus.PaymentFailed,
                Total = intent.Amount
            };

            await _messageBus.PublishMessage(paymentQueueDto, PaymentCreatedTopic);
        }
    }
}
