using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Arcus.Messaging.Pumps.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents an <see cref="IMessageRouter"/> that can route Azure Service Bus <see cref="Message"/>s.
    /// </summary>
    public interface IAzureServiceBusMessageRouter : IMessageRouter
    {
        /// <summary>
        ///     Handle a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <remarks>
        ///     Note that registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s with specific Azure Service Bus operations, will not be able to call those operations
        ///     without an <see cref="MessageReceiver"/>. Use the <see cref="ProcessMessageAsync(MessageReceiver,Message,AzureServiceBusMessageContext,MessageCorrelationInfo,CancellationToken)"/> instead.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>

        Task ProcessMessageAsync(
            Message message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Handle a new message that was received.
        /// </summary>
        /// <param name="messageReceiver">
        ///     The instance that can receive Azure Service Bus <see cref="Message"/>; used within <see cref="IMessageHandler{TMessage,TMessageContext}"/>s with Azure Service Bus specific operations.
        /// </param>
        /// <param name="message">The message that was received by the <paramref name="messageReceiver"/>.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageReceiver"/>, <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>

        Task ProcessMessageAsync(
            MessageReceiver messageReceiver,
            Message message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);
    }
}