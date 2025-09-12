using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging
{
    /// <summary>
    /// Represents a handler for a specific <see cref="ServiceBusReceivedMessage"/> in a <see cref="ServiceBusMessageContext"/>
    /// during the processing of the messages in Azure Service Bus.
    /// </summary>
    public interface IServiceBusMessageHandler<in TMessage> : IMessageHandler<TMessage, ServiceBusMessageContext>
    {
    }
}

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents a handler for a specific <see cref="ServiceBusReceivedMessage"/> in a <see cref="AzureServiceBusMessageContext"/>
    /// during the processing of the messages in Azure Service Bus.
    /// </summary>
    [Obsolete("Will be removed in v4.0, please implement " + nameof(IServiceBusMessageHandler<object>) + " instead")]
    public interface IAzureServiceBusMessageHandler<in TMessage> : IMessageHandler<TMessage, AzureServiceBusMessageContext>
    {
    }
}
