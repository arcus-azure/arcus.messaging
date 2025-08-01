using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents an instance that can route Azure Service Bus <see cref="ServiceBusReceivedMessage"/>s through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s.
    /// </summary>
#pragma warning disable S1133
    [Obsolete("Will be removed in v4.0 since the message router is not publicly exposed anymore")]
#pragma warning restore S1133
    public interface IAzureServiceBusMessageRouter
    {
        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s.
        /// </summary>
        /// <param name="messageReceiver">
        ///     The receiver that can call operations (dead letter, complete...) on an Azure Service Bus <see cref="ServiceBusReceivedMessage"/>.
        /// </param>
        /// <param name="message">The incoming message that needs to be routed through registered message handlers.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageReceiver"/>, <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        Task<MessageProcessingResult> RouteMessageAsync(
            ServiceBusReceiver messageReceiver,
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);
    }
}