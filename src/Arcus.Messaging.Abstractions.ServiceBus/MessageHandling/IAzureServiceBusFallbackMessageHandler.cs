using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Fallback version of the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> to have a safety net when no handlers are found could process the message.
    /// </summary>
    [Obsolete("Will be removed in v3.0, please use the Azure service bus operations on the " + nameof(AzureServiceBusMessageContext) + " instead of defining fallback message handlers")]
    public interface IAzureServiceBusFallbackMessageHandler : IFallbackMessageHandler<ServiceBusReceivedMessage, AzureServiceBusMessageContext>
    {
    }
}
