using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageHandlers
{
    public class WriteSensorToDiskEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorReading>, IFallbackMessageHandler
    {
        private readonly IMessageCorrelationInfoAccessor _correlationAccessor;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSensorToDiskEventHubsMessageHandler" /> class.
        /// </summary>
        public WriteSensorToDiskEventHubsMessageHandler(
            IMessageCorrelationInfoAccessor correlationAccessor,
            ILogger<WriteSensorToDiskEventHubsMessageHandler> logger)
        {
            _correlationAccessor = correlationAccessor;
            _logger = logger;
        }

        public async Task ProcessMessageAsync(
            SensorReading message,
            AzureEventHubsMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            EnsureSameCorrelation(correlationInfo);
            await PublishReadingAsync(message, correlationInfo, cancellationToken);
        }

        public async Task ProcessMessageAsync(
            string message,
            MessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var reading = JsonConvert.DeserializeObject<SensorReading>(message);
            await PublishReadingAsync(reading, correlationInfo, cancellationToken);
        }

        private async Task PublishReadingAsync(
            SensorReading message,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace("Write order v1 message to disk: {MessageId}", message.SensorId);

            string fileName = message.SensorId + ".json";
            string dirPath = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(dirPath, fileName);

            string json = JsonConvert.SerializeObject(
                new SensorReadEventData
                {
                    SensorId = message.SensorId,
                    SensorValue = message.SensorValue,
                    CorrelationInfo = correlationInfo
                });

            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }

        private void EnsureSameCorrelation(MessageCorrelationInfo correlationInfo)
        {
            MessageCorrelationInfo registeredCorrelation = _correlationAccessor.GetCorrelationInfo();
            Assert.NotNull(registeredCorrelation);
            Assert.Equal(registeredCorrelation.OperationId, correlationInfo.OperationId);
            Assert.Equal(registeredCorrelation.TransactionId, correlationInfo.TransactionId);
            Assert.Equal(registeredCorrelation.OperationParentId, correlationInfo.OperationParentId);
            Assert.Equal(registeredCorrelation.CycleId, correlationInfo.CycleId);
        }
    }
}
