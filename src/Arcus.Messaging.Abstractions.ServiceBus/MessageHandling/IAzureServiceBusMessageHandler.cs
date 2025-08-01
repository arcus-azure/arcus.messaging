using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents a handler for a specific <see cref="ServiceBusReceivedMessage"/> in a <see cref="AzureServiceBusMessageContext"/>
    /// during the processing of the messages in Azure Service Bus.
    /// </summary>
#pragma warning disable S1133 // Will be removed in v4.0.
    [Obsolete("Will be removed in v4.0, please use the renamed 'Arcus.Messaging.IServiceBusMessageHandler' instead")]
#pragma warning restore S1133
    public interface IAzureServiceBusMessageHandler<in TMessage> : IMessageHandler<TMessage, AzureServiceBusMessageContext>
    {
    }
}
