using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Abstractions.ServiceBus
{
    /// <summary>
    /// Represents the contextual information concerning an Azure Service Bus message.
    /// </summary>
    public class AzureServiceBusMessageContext : MessageContext
    {
        private readonly ServiceBusReceiver _receiver;
        private readonly ServiceBusReceivedMessage _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageContext"/> class.
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="entityType"></param>
        /// <param name="receiver"></param>
        /// <param name="message"></param>
        protected AzureServiceBusMessageContext(
            string jobId,
            ServiceBusEntityType entityType,
            ServiceBusReceiver receiver,
            ServiceBusReceivedMessage message)
            : base(message.MessageId, jobId, message.ApplicationProperties.ToDictionary(item => item.Key, item => item.Value))
        {
            _receiver = receiver;
            _message = message;

            FullyQualifiedNamespace = receiver.FullyQualifiedNamespace;
            EntityPath = receiver.EntityPath;
            EntityType = entityType;
            SystemProperties = AzureServiceBusSystemProperties.CreateFrom(message);
            LockToken = message.LockToken;
            DeliveryCount = message.DeliveryCount;
        }

        /// <summary>
        /// Gets the fully qualified Azure Service bus namespace that the message pump is associated with.
        /// This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </summary>
        public string FullyQualifiedNamespace { get; }

        /// <summary>
        /// Gets the path of the Azure Service bus entity that the message pump is connected to,
        /// specific to the Azure Service bus namespace that contains it.
        /// </summary>
        public string EntityPath { get; }

        /// <summary>
        /// Gets the type of the Azure Service Bus entity on which the message was received.
        /// </summary>
        public ServiceBusEntityType EntityType { get; }

        /// <summary>
        /// Gets the contextual properties provided on the message provided by the Azure Service Bus runtime
        /// </summary>
        public AzureServiceBusSystemProperties SystemProperties { get; }

        /// <summary>
        /// Gets the token used to lock an individual message for processing
        /// </summary>
        public string LockToken { get; }

        /// <summary>
        /// Gets the amount of times a message was delivered
        /// </summary>
        /// <remarks>This increases when a message is abandoned and re-delivered for processing</remarks>
        public int DeliveryCount { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="AzureServiceBusMessageContext"/> based on the current Azure Service bus situation.
        /// </summary>
        /// <param name="jobId">The unique ID to identity the Azure Service bus message pump that is responsible for pumping messages from the <paramref name="receiver"/>.</param>
        /// <param name="entityType">The type of Azure Service bus entity that the <paramref name="receiver"/> receives from.</param>
        /// <param name="receiver">The Azure Service bus receiver that is responsible for receiving the <paramref name="message"/>.</param>
        /// <param name="message">The Azure Service bus message that is currently being processed.</param>
        /// <exception cref="ArgumentNullException">Thrown when one of the parameters is <c>null</c>.</exception>
        public static AzureServiceBusMessageContext Create(
            string jobId,
            ServiceBusEntityType entityType,
            ServiceBusReceiver receiver,
            ServiceBusReceivedMessage message)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Requires a non-blank job ID to identity an Azure Service bus message pump", nameof(jobId));
            }

            if (receiver is null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return new AzureServiceBusMessageContext(jobId, entityType, receiver, message);
        }

        /// <summary>
        /// Completes the Azure Service Bus message on Azure. This will delete the message from the service.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        public virtual async Task CompleteMessageAsync(CancellationToken cancellationToken)
        {
            EnsureServiceBusFields();

            await _receiver.CompleteMessageAsync(_message, cancellationToken);
        }

        /// <summary>
        /// Dead letters the Azure Service Bus message on Azure with a reason why the message needs to be dead lettered.
        /// </summary>
        /// <param name="deadLetterReason">The reason why the message should be dead lettered.</param>
        /// <param name="deadLetterErrorDescription">The optional extra description of the dead letter error.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized correctly.</exception>
        public virtual async Task DeadLetterMessageAsync(string deadLetterReason, string deadLetterErrorDescription, CancellationToken cancellationToken)
        {
            await DeadLetterMessageAsync(deadLetterReason, deadLetterErrorDescription, newMessageProperties: null, cancellationToken);
        }

        /// <summary>
        /// Dead letters the Azure Service Bus message on Azure while providing <paramref name="newMessageProperties"/> for properties that has to be modified in the process.
        /// </summary>
        /// <param name="deadLetterReason">The reason why the message should be dead lettered.</param>
        /// <param name="deadLetterErrorDescription">The optional extra description of the dead letter error.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <param name="newMessageProperties">The properties to modify on the message during the dead lettering of the message.</param>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        public virtual async Task DeadLetterMessageAsync(string deadLetterReason, string deadLetterErrorDescription, IDictionary<string, object> newMessageProperties, CancellationToken cancellationToken)
        {
            EnsureServiceBusFields();

            await _receiver.DeadLetterMessageAsync(_message, newMessageProperties, deadLetterReason, deadLetterErrorDescription, cancellationToken);
        }

        /// <summary>
        /// <para>
        ///     Abandon the Azure Service Bus message on Azure while providing <paramref name="newMessageProperties"/> for properties that has to be modified in the process.
        /// </para>
        /// <para>
        ///     This will make the message available again for immediate processing as the lock of the message will be released.
        /// </para>
        /// </summary>
        /// <param name="newMessageProperties">The properties to modify on the message during the abandoning of the message.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown when the message context was not initialized correctly.</exception>
        public virtual async Task AbandonMessageAsync(IDictionary<string, object> newMessageProperties, CancellationToken cancellationToken)
        {
            EnsureServiceBusFields();

            await _receiver.AbandonMessageAsync(_message, newMessageProperties, cancellationToken);
        }

        private void EnsureServiceBusFields()
        {
            if (_receiver is null || _message is null)
            {
                throw new InvalidOperationException(
                    "Cannot run Azure Service bus operations on this message context, as it wasn't initialized with a Service bus receiver and message");
            }
        }
    }
}