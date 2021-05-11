using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents a <see cref="IAzureServiceBusMessageHandler{TMessage}"/> template to control the Azure Service Bus message operations during the handling of the deserialized message.
    /// </summary>
    public abstract class AzureServiceBusMessageHandler<TMessage> : AzureServiceBusMessageHandlerTemplate, IAzureServiceBusMessageHandler<TMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageHandlerTemplate"/> class.
        /// </summary>
        protected AzureServiceBusMessageHandler(ILogger logger) : base(logger) { }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public abstract Task ProcessMessageAsync(
            TMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);

        /// <summary>
        /// Completes the Azure Service Bus message on Azure.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task CompleteMessageAsync()
        {
            if (EventArgs is null)
            {
                throw new InvalidOperationException(
                    "Cannot complete the Azure Service Bus message because the message handler running Azure Service Bus-specific operations was not yet initialized correctly");
            }

            Logger.LogTrace("Completing message '{MessageId}'...", EventArgs.Message.MessageId);
            await EventArgs.CompleteMessageAsync(EventArgs.Message, EventArgs.CancellationToken);
            Logger.LogTrace("Message '{MessageId}' is completed!", EventArgs.Message.MessageId);
        }

        /// <summary>
        /// Dead letters the Azure Service Bus message on Azure while providing <paramref name="newMessageProperties"/> for properties that has to be modified in the process.
        /// </summary>
        /// <param name="newMessageProperties">The properties to modify on the message during the dead lettering of the message.</param>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task DeadLetterMessageAsync(IDictionary<string, object> newMessageProperties = null)
        {
            if (EventArgs is null)
            {
                throw new InvalidOperationException(
                    "Cannot dead-letter the Azure Service Bus message because the message handler running Azure Service Bus-specific operations was not yet initialized correctly");
            }

            Logger.LogTrace("Dead-lettering message '{MessageId}' using lock token '{LockToken}'...", EventArgs.Message.MessageId, EventArgs.Message.LockToken);
            await EventArgs.DeadLetterMessageAsync(EventArgs.Message, newMessageProperties, EventArgs.CancellationToken);
            Logger.LogTrace("Message '{MessageId}' is dead-lettered using lock token '{LockToken}'!", EventArgs.Message.MessageId, EventArgs.Message.LockToken);
        }

        /// <summary>
        /// Dead letters the Azure Service Bus message on Azure with a reason why the message needs to be dead lettered.
        /// </summary>
        /// <param name="deadLetterReason">The reason why the message should be dead lettered.</param>
        /// <param name="deadLetterErrorDescription">The optional extra description of the dead letter error.</param>
        /// <exception cref="ArgumentNullException">Thrown when the message is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="deadLetterReason"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task DeadLetterMessageAsync(string deadLetterReason, string deadLetterErrorDescription = null)
        {
            Guard.NotNullOrWhitespace(deadLetterReason, nameof(deadLetterReason), "Requires a non-blank dead letter reason for the message");

            if (EventArgs is null)
            {
                throw new InvalidOperationException(
                    "Cannot dead-letter the Azure Service Bus message because the message handler running Azure Service Bus-specific operations was not yet initialized correctly");
            }

            Logger.LogTrace("Dead-lettering message '{MessageId}' using lock token '{LockToken}' because '{Reason}'...", EventArgs.Message.MessageId, deadLetterReason);
            await EventArgs.DeadLetterMessageAsync(EventArgs.Message, deadLetterReason, deadLetterErrorDescription, EventArgs.CancellationToken);
            Logger.LogTrace("Message '{MessageId}' is dead-lettered using lock token '{LockToken}' because '{Reason}'!", EventArgs.Message.MessageId, deadLetterReason);
        }

        /// <summary>
        /// Abandon the Azure Service Bus message on Azure while providing <paramref name="newMessageProperties"/> for properties that has to be modified in the process.
        /// </summary>
        /// <param name="newMessageProperties">The properties to modify on the message during the abandoning of the message.</param>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task AbandonMessageAsync(IDictionary<string, object> newMessageProperties = null)
        {
            if (EventArgs is null)
            {
                throw new InvalidOperationException(
                    "Cannot abandon the Azure Service Bus message because the message handler running Azure Service Bus-specific operations was not yet initialized correctly");
            }

            Logger.LogTrace("Abandoning message '{MessageId}'...", EventArgs.Message.MessageId);
            await EventArgs.AbandonMessageAsync(EventArgs.Message, newMessageProperties, EventArgs.CancellationToken);
            Logger.LogTrace("Message '{MessageId}' is abandoned using lock token!", EventArgs.Message.MessageId);
        }
    }
}
