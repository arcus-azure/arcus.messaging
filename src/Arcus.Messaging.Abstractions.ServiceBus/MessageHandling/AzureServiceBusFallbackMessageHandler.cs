using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents a <see cref="IAzureServiceBusFallbackMessageHandler"/> template to control the Azure Service Bus message operations during the fallback handling of the message.
    /// </summary>
    [Obsolete("Will be removed in v3.0, please use the Azure service bus operations on the " + nameof(AzureServiceBusMessageContext) + " instead")]
    public abstract class AzureServiceBusFallbackMessageHandler : AzureServiceBusMessageHandlerTemplate, IAzureServiceBusFallbackMessageHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusFallbackMessageHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance to write diagnostic messages during the message handling.</param>
        protected AzureServiceBusFallbackMessageHandler(ILogger logger) : base(logger)
        {
        }

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
        public abstract Task ProcessMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);

        /// <summary>
        /// Completes the Azure Service Bus message on Azure. This will delete the message from the service.
        /// </summary>
        /// <param name="message">The Azure Service Bus message to be completed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task CompleteMessageAsync(ServiceBusReceivedMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (EventArgs is null)
            {
                throw new InvalidOperationException(
                    "Cannot complete the Azure Service Bus message because the message handler running Azure Service Bus-specific operations was not yet initialized correctly");
            }

            Logger.LogTrace("Completing message '{MessageId}'...", message.MessageId);
            await EventArgs.CompleteMessageAsync(message, EventArgs.CancellationToken);
            Logger.LogTrace("Message '{MessageId}' is completed!", message.MessageId);
        }

        /// <summary>
        /// Dead letters the Azure Service Bus <paramref name="message"/> on Azure
        /// while providing <paramref name="newMessageProperties"/> for properties that has to be modified in the process.
        /// </summary>
        /// <param name="message">The message that has to be dead lettered.</param>
        /// <param name="newMessageProperties">The properties to modify on the message during the dead lettering of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> newMessageProperties = null)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (EventArgs is null)
            {
                throw new InvalidOperationException(
                    "Cannot dead-letter the Azure Service Bus message because the message handler running Azure Service Bus-specific operations was not yet initialized correctly");
            }

            Logger.LogTrace("Dead-lettering message '{MessageId}'...", message.MessageId);
            await EventArgs.DeadLetterMessageAsync(message, newMessageProperties, EventArgs.CancellationToken);
            Logger.LogTrace("Message '{MessageId}' is dead-lettered!", message.MessageId);
        }

        /// <summary>
        /// Dead letters the Azure Service Bus <paramref name="message"/> on Azure with a reason why the message needs to be dead lettered.
        /// </summary>
        /// <param name="message">The message that has to be dead lettered.</param>
        /// <param name="deadLetterReason">The reason why the <paramref name="message"/> should be dead lettered.</param>
        /// <param name="deadLetterErrorDescription">The optional extra description of the dead letter error.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="deadLetterReason"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, string deadLetterReason, string deadLetterErrorDescription = null)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(deadLetterReason))
            {
                throw new ArgumentException("Requires a non-blank dead letter reason for the message", nameof(deadLetterReason));
            }

            if (EventArgs is null)
            {
                throw new InvalidOperationException(
                    "Cannot dead-letter the Azure Service Bus message because the message handler running Azure Service Bus-specific operations was not yet initialized correctly");
            }

            Logger.LogTrace("Dead-lettering message '{MessageId}' because '{Reason}'...", message.MessageId, deadLetterReason);
            await EventArgs.DeadLetterMessageAsync(message, deadLetterReason, deadLetterErrorDescription, EventArgs.CancellationToken);
            Logger.LogTrace("Message '{MessageId}' is dead-lettered because '{Reason}'!", message.MessageId, deadLetterReason);
        }

        /// <summary>
        /// <para>
        ///     Abandon the Azure Service Bus <paramref name="message"/> on Azure
        ///     while providing <paramref name="newMessageProperties"/> for properties that has to be modified in the process.
        /// </para>
        /// <para>
        ///     This will make the message available again for immediate processing as the lock of the message will be released.
        /// </para>
        /// </summary>
        /// <param name="message">The message that has to be abandoned.</param>
        /// <param name="newMessageProperties">The properties to modify on the message during the abandoning of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> newMessageProperties = null)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (EventArgs is null)
            {
                throw new InvalidOperationException(
                    "Cannot abandon the Azure Service Bus message because the message handler running Azure Service Bus-specific operations was not yet initialized correctly");
            }

            Logger.LogTrace("Abandoning message '{MessageId}'...", message.MessageId);
            await EventArgs.AbandonMessageAsync(message, newMessageProperties, EventArgs.CancellationToken);
            Logger.LogTrace("Message '{MessageId}' is abandoned!", message.MessageId);
        }
    }
}
