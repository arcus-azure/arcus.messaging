using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class ShipmentAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Shipment>
    {
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
        public Task ProcessMessageAsync(
            Shipment message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
