using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Microsoft.Azure.ServiceBus;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents a handler for a specific <see cref="Message"/> in a <see cref="AzureServiceBusMessageContext"/>
    /// during the processing of the <see cref="AzureServiceBusMessagePump"/>.
    /// </summary>
    public interface IAzureServiceBusMessageHandler<in TMessage> : IMessageHandler<TMessage, AzureServiceBusMessageContext>
    {
    }
}
