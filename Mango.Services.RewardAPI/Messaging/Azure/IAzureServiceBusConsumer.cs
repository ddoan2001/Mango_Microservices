namespace Mango.Services.RewardAPI.Messaging.Azure
{
    public interface IAzureServiceBusConsumer
    {
        Task Start();
        Task Stop();
    }
}
