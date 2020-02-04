using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Plugin to provide correlation information at outgoing Azure Service Bus messages.
    /// </summary>
    public class OutboundCorrelationInfoServiceBusPlugin : ServiceBusPlugin
    {
        private readonly MessageCorrelationInfo _correlationInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutboundCorrelationInfoServiceBusPlugin"/> class.
        /// </summary>
        /// <param name="correlationInfo">The current correlation information.</param>
        public OutboundCorrelationInfoServiceBusPlugin(MessageCorrelationInfo correlationInfo)
        {
            Guard.NotNull(correlationInfo, nameof(correlationInfo));

            _correlationInfo = correlationInfo;
        }

        /// <summary>
        /// Gets the unique name of the plugin.
        /// </summary>
        public override string Name => nameof(OutboundCorrelationInfoServiceBusPlugin);

        /// <summary>
        /// Enrich the specified <paramref name="message"/> with the correlation info available at the moment.
        /// </summary>
        /// <param name="message">The message to enrich with the correlation information.</param>
        public override Task<Message> BeforeMessageSend(Message message)
        {
            message.CorrelationId = _correlationInfo.OperationId;

            if (!String.IsNullOrEmpty(_correlationInfo.TransactionId))
            {
                message.UserProperties.Add(PropertyNames.TransactionId, _correlationInfo.TransactionId);
            }

            return Task.FromResult(message);
        }
    }
}
