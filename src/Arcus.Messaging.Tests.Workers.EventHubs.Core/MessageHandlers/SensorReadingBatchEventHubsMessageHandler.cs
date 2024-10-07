using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageHandlers
{
    public class SensorReadingBatchEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorReadingBatch>
    {
        private readonly WriteSensorToDiskEventHubsMessageHandler _innerHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorReadingBatchEventHubsMessageHandler" /> class.
        /// </summary>
        public SensorReadingBatchEventHubsMessageHandler(
            IMessageCorrelationInfoAccessor correlationAccessor,
            ILogger<WriteSensorToDiskEventHubsMessageHandler> logger)
        {
            _innerHandler = new WriteSensorToDiskEventHubsMessageHandler(correlationAccessor, logger);
        }

        public async Task ProcessMessageAsync(
            SensorReadingBatch message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            foreach (var reading in message.Readings)
            {
                await _innerHandler.ProcessMessageAsync(reading, messageContext, correlationInfo, cancellationToken);
            }
        }
    }
}
