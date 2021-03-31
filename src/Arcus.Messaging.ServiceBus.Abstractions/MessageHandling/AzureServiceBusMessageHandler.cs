using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.ServiceBus.Abstractions.MessageHandling
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
        protected async Task CompleteMessageAsync()
        {
            if (LockToken is null)
            {
                throw new InvalidOperationException("Cannot complete the message because the message receiver was not yet initialized");
            }

            await MessageReceiver.CompleteAsync(LockToken);
        }

        /// <summary>
        /// Dead letters the Azure Service Bus message on Azure while providing <paramref name="newMessageProperties"/> for properties that has to be modified in the process.
        /// </summary>
        /// <param name="newMessageProperties">The properties to modify on the message during the dead lettering of the message.</param>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task DeadLetterMessageAsync(IDictionary<string, object> newMessageProperties = null)
        {
            if (LockToken is null)
            {
                throw new InvalidOperationException("Cannot dead letter the message because the lock token was not yet initialized");
            }

            if (MessageReceiver is null)
            {
                throw new InvalidOperationException("Cannot dead letter the message because the message receiver was not yet initialized");
            }

            Logger.LogTrace("Dead-lettering message using lock token '{LockToken}'...", LockToken);
            await MessageReceiver.DeadLetterAsync(LockToken, newMessageProperties);
            Logger.LogTrace("Message was dead-lettered using lock token '{LockToken}'!", LockToken);
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

            if (LockToken is null)
            {
                throw new InvalidOperationException("Cannot dead letter the message because the lock token was not yet initialized");
            }

            if (MessageReceiver is null)
            {
                throw new InvalidOperationException("Cannot dead letter the message because the message receiver was not yet initialized");
            }

            Logger.LogTrace("Dead-lettering message using lock token '{LockToken}' because '{Reason}'...", LockToken, deadLetterReason);
            await MessageReceiver.DeadLetterAsync(LockToken, deadLetterReason, deadLetterErrorDescription);
            Logger.LogTrace("Message was dead-lettered using lock token '{LockToken}' because '{Reason}'!", LockToken, deadLetterReason);
        }

        /// <summary>
        /// Abandon the Azure Service Bus message on Azure while providing <paramref name="newMessageProperties"/> for properties that has to be modified in the process.
        /// </summary>
        /// <param name="newMessageProperties">The properties to modify on the message during the abandoning of the message.</param>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        protected async Task AbandonMessageAsync(IDictionary<string, object> newMessageProperties = null)
        {
            if (LockToken is null)
            {
                throw new InvalidOperationException("Cannot abandon the message because the lock token was not yet initialized");
            }

            if (MessageReceiver is null)
            {
                throw new InvalidOperationException("Cannot Abandon the message because the message receiver was not yet initialized");
            }

            Logger.LogTrace("Abandoning message using lock token '{LockToken}'...", LockToken);
            await MessageReceiver.AbandonAsync(LockToken, newMessageProperties);
            Logger.LogTrace("Message was abandoned using lock token '{LockToken}'!", LockToken);
        }
    }
}
