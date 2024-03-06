using System;
using Arcus.Messaging.Abstractions.EventHubs;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.EventHubs
{
    public static class EventDataExtensions
    {
        /// <summary>
        /// Gets the message contextual information from the received <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The consumed Azure EventHubs event to describe the messaging context.</param>
        /// <param name="eventProcessor">The Azure EventHubs processor that received the <paramref name="message"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> or the <paramref name="eventProcessor"/> is <c>null</c>.</exception>
        public static AzureEventHubsMessageContext GetMessageContext(this EventData message, EventProcessorClient eventProcessor)
        {
            Guard.NotNull(message, nameof(message), "Requires an event data to retrieve the Azure EventHubs message context");
            Guard.NotNull(eventProcessor, nameof(eventProcessor), "Requires an Azure EventHubs event processor to retrieve the information to create a messaging context for the consumed event");

            return AzureEventHubsMessageContext.CreateFrom(message, eventProcessor, jobId: Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Gets the message contextual information from the received <paramref name="message"/>.
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
        ///     Thrown when the <paramref name="eventHubsNamespace"/>, <paramref name="consumerGroup"/>, <paramref name="eventHubsName"/> is blank.
        /// </exception>
        public static AzureEventHubsMessageContext GetMessageContext(this EventData message, string eventHubsNamespace, string consumerGroup, string eventHubsName)
        {
            Guard.NotNull(message, nameof(message), "Requires an event data to retrieve the Azure EventHubs message context");
            Guard.NotNullOrWhitespace(eventHubsNamespace, nameof(eventHubsNamespace), "Requires an Azure EventHubs namespace to built-up the message context");
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires an Azure EventHubs name to built-up the message context");
            Guard.NotNullOrWhitespace(consumerGroup, nameof(consumerGroup), "Requires an Azure EventHubs consumer group to built-up the message context");

            return AzureEventHubsMessageContext.CreateFrom(message, eventHubsNamespace, consumerGroup, eventHubsName, jobId: Guid.NewGuid().ToString());
        }
    }
}
