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

            if (receiver != null)
            {
                FullyQualifiedNamespace = receiver.FullyQualifiedNamespace;
                EntityPath = receiver.EntityPath;
            }

            EntityType = entityType;
            SystemProperties = AzureServiceBusSystemProperties.CreateFrom(message);
            LockToken = message.LockToken;
            DeliveryCount = message.DeliveryCount;
        }

        /// <summary>
        /// Gets the fully qualified Azure Service bus namespace that the message pump is associated with.
        /// This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </summary>
        public string FullyQualifiedNamespace { get; init; }

        /// <summary>
        /// Gets the path of the Azure Service bus entity that the message pump is connected to,
        /// specific to the Azure Service bus namespace that contains it.
        /// </summary>
        public string EntityPath { get; init; }

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
            ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
            ArgumentNullException.ThrowIfNull(receiver);
            ArgumentNullException.ThrowIfNull(message);

            return new AzureServiceBusMessageContext(jobId, entityType, receiver, message);
        }

        /// <summary>
        /// Completes the Azure Service Bus message on Azure. This will delete the message from the service.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the message handler was not initialized yet.</exception>
        public virtual async Task CompleteMessageAsync(CancellationToken cancellationToken)
        {
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
            await _receiver.AbandonMessageAsync(_message, newMessageProperties, cancellationToken);
        }
    }

    /// <summary>
    /// Represents a collection used to store properties which are set by the Service Bus service.
    /// </summary>
    public class AzureServiceBusSystemProperties
    {
        private AzureServiceBusSystemProperties(ServiceBusReceivedMessage message)
        {
            DeadLetterSource = message.DeadLetterSource;
            DeliveryCount = message.DeliveryCount;
            EnqueuedSequenceNumber = message.EnqueuedSequenceNumber;
            EnqueuedTime = message.EnqueuedTime;
            LockToken = message.LockToken;
            IsLockTokenSet = message.LockToken != null && message.LockToken != Guid.Empty.ToString();
            LockedUntil = message.LockedUntil;
            IsReceived = message.SequenceNumber > -1;
            SequenceNumber = message.SequenceNumber;
            ContentType = message.ContentType;
        }

        /// <summary>
        /// Creates an <see cref="AzureServiceBusSystemProperties"/> instance based on the available information provided in the Azure Service Bus <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The received Azure Service Bus message containing the system-related information.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public static AzureServiceBusSystemProperties CreateFrom(ServiceBusReceivedMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return new AzureServiceBusSystemProperties(message);
        }

        /// <summary>
        /// Gets the name of the queue or subscription that this message was enqueued on, before it was dead-lettered.
        /// </summary>
        /// <remarks>
        /// 	Only set in messages that have been dead-lettered and subsequently auto-forwarded from the dead-letter queue
        ///     to another entity. Indicates the entity in which the message was dead-lettered. This property is read-only.
        /// </remarks>
        public string DeadLetterSource { get; }

        /// <summary>
        /// Get the current delivery count.
        /// </summary>
        /// <value>This value starts at 1.</value>
        /// <remarks>
        ///    Number of deliveries that have been attempted for this message. The count is incremented when a message lock expires,
        ///    or the message is explicitly abandoned by the receiver. This property is read-only.
        /// </remarks>
        public int DeliveryCount { get; }

        /// <summary>
        /// Gets the unique number assigned to a message by Service Bus.
        /// </summary>
        /// <remarks>
        ///     The sequence number is a unique 64-bit integer assigned to a message as it is accepted
        ///     and stored by the broker and functions as its true identifier. For partitioned entities,
        ///     the topmost 16 bits reflect the partition identifier. Sequence numbers monotonically increase.
        ///     They roll over to 0 when the 48-64 bit range is exhausted. This property is read-only.
        /// </remarks>
        public long SequenceNumber { get; }

        /// <summary>
        /// Specifies if the message has been obtained from the broker.
        /// </summary>
        public bool IsReceived { get; }

        /// <summary>
        /// Gets or sets the original sequence number of the message.
        /// </summary>
        /// <value>The enqueued sequence number of the message.</value>
        /// <remarks>
        /// For messages that have been auto-forwarded, this property reflects the sequence number
        /// that had first been assigned to the message at its original point of submission. This property is read-only.
        /// </remarks>
        public long EnqueuedSequenceNumber { get; }

        /// <summary>
        /// Gets or sets the date and time of the sent time in UTC.
        /// </summary>
        /// <value>The enqueue time in UTC. </value>
        /// <remarks>
        ///    The UTC instant at which the message has been accepted and stored in the entity.
        ///    This value can be used as an authoritative and neutral arrival time indicator when
        ///    the receiver does not want to trust the sender's clock. This property is read-only.
        /// </remarks>
        public DateTimeOffset EnqueuedTime { get; }

        /// <summary>
        /// Gets the lock token for the current message.
        /// </summary>
        /// <remarks>
        ///   The lock token is a reference to the lock that is being held by the broker in ReceiveMode.PeekLock mode.
        ///   Locks are used to explicitly settle messages as explained in the <a href="https://docs.microsoft.com/azure/service-bus-messaging/message-transfers-locks-settlement">product documentation in more detail</a>.
        ///   The token can also be used to pin the lock permanently through the <a href="https://docs.microsoft.com/azure/service-bus-messaging/message-deferral">Deferral API</a> and, with that, take the message out of the
        ///   regular delivery state flow. This property is read-only.
        /// </remarks>
        public string LockToken { get; }

        /// <summary>
        /// Specifies whether or not there is a lock token set on the current message.
        /// </summary>
        /// <remarks>A lock token will only be specified if the message was received using PeekLock.</remarks>
        public bool IsLockTokenSet { get; }

        /// <summary>
        /// Gets the date and time in UTC until which the message will be locked in the queue/subscription.
        /// </summary>
        /// <value>The date and time until which the message will be locked in the queue/subscription.</value>
        /// <remarks>
        /// 	For messages retrieved under a lock (peek-lock receive mode, not pre-settled) this property reflects the UTC
        ///     instant until which the message is held locked in the queue/subscription. When the lock expires, the <see cref="P:Azure.Messaging.ServiceBus.ServiceBusReceivedMessage.DeliveryCount" />
        ///     is incremented and the message is again available for retrieval. This property is read-only.
        /// </remarks>
        public DateTimeOffset LockedUntil { get; }

        /// <summary>
        /// Gets the content type descriptor.
        /// </summary>
        /// <value>RFC2045 Content-Type descriptor.</value>
        /// <remarks>
        /// Optionally describes the payload of the message,
        /// with a descriptor following the format of RFC2045, Section 5, for example "application/json".
        /// </remarks>
        public string ContentType { get; }
    }
}