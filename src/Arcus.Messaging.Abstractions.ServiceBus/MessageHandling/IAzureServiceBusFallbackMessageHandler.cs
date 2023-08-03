using Arcus.Messaging.Abstractions.MessageHandling;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Fallback version of the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> to have a safety net when no handlers are found could process the message.
    /// </summary>
    public interface IAzureServiceBusFallbackMessageHandler : IFallbackMessageHandler<ServiceBusReceivedMessage, AzureServiceBusMessageContext>
    {
    }
}
