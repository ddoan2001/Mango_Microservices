namespace Mango.Services.OrderAPI.Messaging.Azure
{
    public interface IAzureServiceBusConsumer
    {
        Task Start();
        Task Stop();
    }
}
