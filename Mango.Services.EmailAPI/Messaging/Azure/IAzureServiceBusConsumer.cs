namespace Mango.Services.EmailAPI.Messaging.Azure
{
    public interface IAzureServiceBusConsumer
    {
        Task Start();
        Task Stop();
    }
}
