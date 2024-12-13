using System;
using Azure.Messaging.ServiceBus;
using GuardNet;

namespace Arcus.Messaging.Abstractions.ServiceBus
{
    /// <summary>
    /// Represents a collection used to store properties which are set by the Service Bus service.
    /// </summary>
    public class AzureServiceBusSystemProperties
    {
        private AzureServiceBusSystemProperties(ServiceBusReceivedMessage message)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus received message to construct a set of Azure Service Bus system properties");
            
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
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus received message to construct a set of Azure Service Bus system properties");

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
