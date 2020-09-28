using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents a <see cref="IAzureServiceBusFallbackMessageHandler"/> template to control the Azure Service Bus message operations during the fallback handling of the message.
    /// </summary>
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
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">The Azure Service Bus Message message that was received</param>
        /// <param name="messageContext">The context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     The information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers.
        /// </param>
        /// <param name="cancellationToken">The cancellation token to cancel the processing.</param>
        public abstract Task ProcessMessageAsync(
            Message message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);

        /// <summary>
        /// Dead letters the Azure Service Bus <paramref name="message"/> on Azure while providing <paramref name="newMessageProperties"/> for properties that has to be modified in the process.
        /// </summary>
        /// <param name="message">The message that has to be dead lettered.</param>
        /// <param name="newMessageProperties">The properties to modify on the message during the dead lettering of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task DeadLetterAsync(Message message, IDictionary<string, object> newMessageProperties = null)
        {
            Guard.NotNull(message, nameof(message), "Requires a message to be dead lettered");

            if (MessageReceiver is null)
            {
                throw new InvalidOperationException($"Cannot DeadLetter the message '{message.MessageId}' because the message receiver was not yet initialized yet");
            }

            Logger.LogTrace("Dead-lettering message '{MessageId}'...", message.MessageId);
            await MessageReceiver.DeadLetterAsync(message.SystemProperties.LockToken, newMessageProperties);
            Logger.LogTrace("Message '{MessageId}' was dead-lettered", message.MessageId);
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
        protected async Task DeadLetterAsync(Message message, string deadLetterReason, string deadLetterErrorDescription = null)
        {
            Guard.NotNull(message, nameof(message), "Requires a message to be dead lettered");
            Guard.NotNullOrWhitespace(deadLetterReason, nameof(deadLetterReason), "Requires a non-blank dead letter reason for the message");

            if (MessageReceiver is null)
            {
                throw new InvalidOperationException($"Cannot DeadLetter the message '{message.MessageId}' because the message receiver was not yet initialized yet");
            }

            Logger.LogTrace("Dead-lettering message '{MessageId}'...", message.MessageId);
            await MessageReceiver.DeadLetterAsync(message.SystemProperties.LockToken, deadLetterReason, deadLetterErrorDescription);
            Logger.LogTrace("Message '{MessageId}' was dead-lettered!", message.MessageId);
        }

        /// <summary>
        /// Abandon the Azure Service Bus <paramref name="message"/> on Azure while providing <paramref name="newMessageProperties"/> for properties that has to be modified in the process.
        /// </summary>
        /// <param name="message">The message that has to be abandoned.</param>
        /// <param name="newMessageProperties">The properties to modify on the message during the abandoning of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task AbandonAsync(Message message, IDictionary<string, object> newMessageProperties = null)
        {
            Guard.NotNull(message, nameof(message), "Requires a message to be abandoned");

            if (MessageReceiver is null)
            {
                throw new InvalidOperationException($"Cannot Abandon the message '{message.MessageId}' because the message receiver was not yet initialized yet");
            }

            Logger.LogTrace("Abandoning message '{MessageId}'...", message.MessageId);
            await MessageReceiver.AbandonAsync(message.SystemProperties.LockToken, newMessageProperties);
            Logger.LogTrace("Message '{MessageId}' was abandoned", message.MessageId);
        }
    }
}
