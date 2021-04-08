﻿using Arcus.Messaging.Abstractions.MessageHandling;
using Microsoft.Azure.ServiceBus;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents a handler for a specific <see cref="Message"/> in a <see cref="AzureServiceBusMessageContext"/>
    /// during the processing of the messages in Azure Service Bus.
    /// </summary>
    public interface IAzureServiceBusMessageHandler<in TMessage> : IMessageHandler<TMessage, AzureServiceBusMessageContext>
    {
    }
}
