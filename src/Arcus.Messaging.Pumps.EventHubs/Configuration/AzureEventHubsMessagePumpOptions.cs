using System;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using GuardNet;

namespace Arcus.Messaging.Pumps.EventHubs.Configuration
{
    /// <summary>
    /// Represents the additional set of options to configure the behavior of the <see cref="AzureEventHubsMessagePump"/>.
    /// </summary>
    public class AzureEventHubsMessagePumpOptions
    {
        private string _consumerGroup = "$Default";

        /// <summary>
        /// Gets or sets the name of the consumer group this processor is associated with. Events are read in the context of this group. (Default: <c>"$Default"</c>).
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public string ConsumerGroup
        {
            get => _consumerGroup;
            set
            {
                Guard.NotNullOrWhitespace(value, nameof(value), "Requires a non-blank Azure EventHubs consumer group to consume event messages from");
                _consumerGroup = value;
            }
        }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the message router.
        /// </summary>
        public AzureEventHubsMessageRouterOptions Routing { get; } = new AzureEventHubsMessageRouterOptions();
    }
}
