using Arcus.Messaging.Abstractions.MessageHandling;
using Azure.Messaging.EventHubs;

namespace Arcus.Messaging.Abstractions.EventHubs.MessageHandling
{
    /// <summary>
    /// Represents a handler for a specific <see cref="EventData"/> in a <see cref="AzureEventHubsMessageContext"/>
    /// during the processing of the messages in Azure EventHubs.
    /// </summary>
    public interface IAzureEventHubsMessageHandler<in TMessage> : IMessageHandler<TMessage, AzureEventHubsMessageContext>
    {
    }
}
