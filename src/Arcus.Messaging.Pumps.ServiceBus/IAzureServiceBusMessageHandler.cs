using Arcus.Messaging.Pumps.Abstractions;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents a handler for a specific <typeparamref name="TMessage"/> in a <see cref="AzureServiceBusMessageContext"/>
    /// during the processing of the <see cref="AzureServiceBusMessagePump{TMessage}"/>.
    /// </summary>
    public interface IAzureServiceBusMessageHandler<in TMessage> : IMessageHandler<TMessage, AzureServiceBusMessageContext>
    {
    }
}
