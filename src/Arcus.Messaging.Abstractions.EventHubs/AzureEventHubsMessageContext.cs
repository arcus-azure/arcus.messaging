using System;
using System.Collections.Generic;
using System.Threading;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using GuardNet;

namespace Arcus.Messaging.Abstractions.EventHubs
{
    /// <summary>
    /// Represents the contextual information concerning an Azure EventHubs <see cref="EventData"/> message
    /// </summary>
    public class AzureEventHubsMessageContext : MessageContext
    {
        private AzureEventHubsMessageContext(
            EventData eventData, 
            string eventHubsName,
            string consumerGroup,
            string eventHubsNamespace,
            string jobId) : base(eventData.MessageId ?? Guid.NewGuid().ToString(), jobId, eventData.Properties)
        {
            EventHubsName = eventHubsName;
            ConsumerGroup = consumerGroup;
            EventHubsNamespace = eventHubsNamespace;
            ContentType = eventData.ContentType;
            SystemProperties = eventData.SystemProperties;
            SequenceNumber = eventData.SequenceNumber;
            Offset = eventData.Offset;
            EnqueueTime = eventData.EnqueuedTime;
            PartitionKey = eventData.PartitionKey;
        }

        /// <summary>
        /// Gets the name of the Event Hub that the processor is connected to, specific to the Event Hubs namespace that contains it.
        /// </summary>
        public string EventHubsName { get; }

        /// <summary>
        /// Gets the name of the consumer group this event processor is associated with.
        /// Events will be read only in the context of this group.
        /// </summary>
        public string ConsumerGroup { get; }

        /// <summary>
        /// Gets the fully qualified Event Hubs namespace that the processor is associated with.
        /// This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </summary>
        public string EventHubsNamespace { get; }

        /// <summary>
        ///     Gets he set of free-form event properties which were provided by the Event Hubs service
        ///     to pass metadata associated with the event or associated Event Hubs operation.
        /// </summary>
        /// <value>
        ///     These properties are read-only and will only be populated for events that have been read from Event Hubs.
        ///     The default value when not populated is an empty set.
        /// </value>
        public IReadOnlyDictionary<string, object> SystemProperties { get; }

        /// <summary>
        ///     Gets the MIME type describing the data contained in the <see cref="EventData.EventBody" />,
        ///     intended to allow consumers to make informed decisions for inspecting and processing the event.
        /// </summary>
        /// <value>
        ///     The MIME type of the <see cref="EventData.EventBody" /> content; when unknown,
        ///     it is recommended that this value should not be set.  When the body is known to be
        ///     truly opaque binary data, it is recommended that "application/octet-stream" be used.
        /// </value>
        /// <remarks>
        ///     <para>The <see cref="EventData.ContentType" /> is managed by the application and is intended to allow coordination between event producers and consumers.</para>
        ///     <para>Event Hubs does not read, generate, or populate this value.  It does not influence how Event Hubs stores or manages the event.</para>
        /// </remarks>
        /// <seealso href="https://datatracker.ietf.org/doc/html/rfc2046">RFC2046 (MIME Types)</seealso>
        public string ContentType { get; }

        /// <summary>
        ///     Gets the sequence number assigned to the event when it was enqueued in the associated Event Hub partition.
        /// </summary>
        /// <value>
        ///     This value is read-only and will only be populated for events that have been read from Event Hubs.
        ///     The default value when not populated is <see cref="Int64.MinValue" />.
        /// </value>
        public long SequenceNumber { get; }

        /// <summary>
        ///     Gets he offset of the event when it was received from the associated Event Hub partition.
        /// </summary>
        /// <value>
        ///     This value is read-only and will only be populated for events that have been read from Event Hubs.
        ///     The default value when not populated is <see cref="Int64.MinValue" />.
        /// </value>
        public long Offset { get; }

        /// <summary>
        ///     Gets the date and time, in UTC, of when the event was enqueued in the Event Hub partition.
        /// </summary>
        /// <value>
        ///     This value is read-only and will only be populated for events that have been read from Event Hubs.
        ///     The default value when not populated is <c>default(DateTimeOffset)</c>.
        /// </value>
        public DateTimeOffset EnqueueTime { get; }

        /// <summary>
        ///     Gets the partition hashing key applied to the batch that the associated <see cref="EventData" />, was published with.
        /// </summary>
        /// <value>
        ///     This value is read-only and will only be populated for events that have been read from Event Hubs.
        ///     The default value when not populated is <c>null</c>.
        /// </value>
        /// <remarks>
        ///     To specify a partition key when publishing an event, specify your key in the <see cref="SendEventOptions" />
        ///     and use the <see cref="EventHubProducerClient.SendAsync(IEnumerable{EventData},SendEventOptions,CancellationToken)" /> overload.
        /// </remarks>
        public string PartitionKey { get; }

        /// <summary>
        /// Creates an <see cref="AzureEventHubsMessageContext"/> instance based on the information from the Azure EventHubs <paramref name="message"/>
        /// and <paramref name="eventProcessor"/> from where the message originates from.
        /// </summary>
        /// <param name="message">The consumed Azure EventHubs event to describe the messaging context.</param>
        /// <param name="eventProcessor">The Azure EventHubs processor that received the <paramref name="message"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> or <paramref name="eventProcessor"/> is <c>null</c>.</exception>
        [Obsolete("Use the other factory method with the job ID to link this message context to a message pump")]
        public static AzureEventHubsMessageContext CreateFrom(
            EventData message,
            EventProcessorClient eventProcessor)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure EventHubs event data message to retrieve the information to create a messaging context for this message");
            Guard.NotNull(eventProcessor, nameof(eventProcessor), "Requires an Azure EventHubs event processor to retrieve the information to create a messaging context for the consumed event");

            return CreateFrom(
                message,
                eventProcessor.FullyQualifiedNamespace,
                eventProcessor.ConsumerGroup,
                eventProcessor.EventHubName,
                "<not-defined>");
        }

        /// <summary>
        /// Creates an <see cref="AzureEventHubsMessageContext"/> instance based on the information from the Azure EventHubs <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The consumed Azure EventHubs event to describe the messaging context.</param>
        /// <param name="eventHubsNamespace">
        ///     The fully qualified Event Hubs namespace that the processor is associated with.
        ///     This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="consumerGroup">The name of the consumer group this event processor is associated with.</param>
        /// <param name="eventHubsName"> The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventHubsNamespace"/>, <paramref name="consumerGroup"/>, or <paramref name="eventHubsName"/> is blank.
        /// </exception>
        [Obsolete("Use the other factory method with the job ID to link this message context to a message pump")]
        public static AzureEventHubsMessageContext CreateFrom(
            EventData message,
            string eventHubsNamespace,
            string consumerGroup,
            string eventHubsName)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure EventHubs event data message to retrieve the information to create a messaging context for this message");
            Guard.NotNullOrWhitespace(eventHubsNamespace, nameof(eventHubsNamespace), "Requires a non-blank Azure EventHubs fully qualified namespace to relate the messaging context to the event message");
            Guard.NotNullOrWhitespace(consumerGroup, nameof(consumerGroup), "Requires a non-blank Azure EventHubs consumer group to relate the messaging context to the event message");
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires a non-blank Azure EventHubs name to relate the messaging context to the event message");

            return CreateFrom(message, eventHubsNamespace, consumerGroup, eventHubsName, "<not-defined>");
        }

        /// <summary>
        /// Creates an <see cref="AzureEventHubsMessageContext"/> instance based on the information from the Azure EventHubs <paramref name="message"/>
        /// and <paramref name="eventProcessor"/> from where the message originates from.
        /// </summary>
        /// <param name="message">The consumed Azure EventHubs event to describe the messaging context.</param>
        /// <param name="eventProcessor">The Azure EventHubs processor that received the <paramref name="message"/>.</param>
        /// <param name="jobId">The unique identifier of the message pump that processes the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> or <paramref name="eventProcessor"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="jobId"/> is blank.</exception>
        public static AzureEventHubsMessageContext CreateFrom(
            EventData message,
            EventProcessorClient eventProcessor,
            string jobId)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure EventHubs event data message to retrieve the information to create a messaging context for this message");
            Guard.NotNull(eventProcessor, nameof(eventProcessor), "Requires an Azure EventHubs event processor to retrieve the information to create a messaging context for the consumed event");
            Guard.NotNullOrWhitespace(jobId, nameof(jobId), "Requires a non-blank job ID to link this message context to a message pump that processes the event message");

            return CreateFrom(
                message,
                eventProcessor.FullyQualifiedNamespace,
                eventProcessor.ConsumerGroup,
                eventProcessor.EventHubName,
                jobId);
        }

        /// <summary>
        /// Creates an <see cref="AzureEventHubsMessageContext"/> instance based on the information from the Azure EventHubs <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The consumed Azure EventHubs event to describe the messaging context.</param>
        /// <param name="eventHubsNamespace">
        ///     The fully qualified Event Hubs namespace that the processor is associated with.
        ///     This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="consumerGroup">The name of the consumer group this event processor is associated with.</param>
        /// <param name="eventHubsName"> The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.</param>
        /// <param name="jobId">The unique identifier of the message pump that processes the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventHubsNamespace"/>, <paramref name="consumerGroup"/>, <paramref name="eventHubsName"/>, or <paramref name="jobId"/> is blank.
        /// </exception>
        public static AzureEventHubsMessageContext CreateFrom(
            EventData message,
            string eventHubsNamespace,
            string consumerGroup,
            string eventHubsName,
            string jobId)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure EventHubs event data message to retrieve the information to create a messaging context for this message");
            Guard.NotNullOrWhitespace(eventHubsNamespace, nameof(eventHubsNamespace), "Requires a non-blank Azure EventHubs fully qualified namespace to relate the messaging context to the event message");
            Guard.NotNullOrWhitespace(consumerGroup, nameof(consumerGroup), "Requires a non-blank Azure EventHubs consumer group to relate the messaging context to the event message");
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires a non-blank Azure EventHubs name to relate the messaging context to the event message");
            Guard.NotNullOrWhitespace(jobId, nameof(jobId), "Requires a non-blank job ID to link this message context to a message pump that processes the event message");

            return new AzureEventHubsMessageContext(message, eventHubsName, consumerGroup, eventHubsNamespace, jobId);
        }
    }
}
