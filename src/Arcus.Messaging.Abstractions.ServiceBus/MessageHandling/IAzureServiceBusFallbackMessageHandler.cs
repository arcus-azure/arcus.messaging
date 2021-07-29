using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Fallback version of the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> to have a safety net when no handlers are found could process the message.
    /// </summary>
    public interface IAzureServiceBusFallbackMessageHandler
    {
        /// <summary>
        /// Process a new Azure Service Bus message that was received but couldn't be handled by any of the general registered message handlers.
        /// </summary>
        /// <param name="message">The Azure Service Bus Message message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, the <paramref name="messageContext"/>, or the <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        Task ProcessMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);
    }
}
