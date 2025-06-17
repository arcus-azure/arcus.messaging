using System;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;

#pragma warning disable S1133 // Disable usage of deprecated functionality until v3.0 is released.

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// Represents the user-configurable options to control the correlation information tracking
    /// during the receiving of the Azure Service Bus messages in the <see cref="AzureServiceBusMessagePump"/>.
    /// </summary>
    [Obsolete("Will be removed in v3.0 as the options model is only used in the deprecated 'Hierarchical' correlation format")]
    public class AzureServiceBusCorrelationOptions
    {
        private readonly MessageCorrelationOptions _correlationOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusCorrelationOptions" /> class.
        /// </summary>
        public AzureServiceBusCorrelationOptions() : this(new MessageCorrelationOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusCorrelationOptions" /> class.
        /// </summary>
        internal AzureServiceBusCorrelationOptions(MessageCorrelationOptions correlationOptions)
        {
            _correlationOptions = correlationOptions;
        }

        /// <summary>
        /// Gets or sets the message correlation format of the received Azure Service Bus message.
        /// </summary>
        public MessageCorrelationFormat Format
        {
            get => _correlationOptions.Format;
            set => _correlationOptions.Format = value;
        }
    }
}
