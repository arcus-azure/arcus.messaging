using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class WriteOrderV2ToDiskAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<OrderV2>
    {
        private readonly ILogger<WriteOrderV2ToDiskAzureServiceBusMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteOrderV2ToDiskAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public WriteOrderV2ToDiskAzureServiceBusMessageHandler(
            ILogger<WriteOrderV2ToDiskAzureServiceBusMessageHandler> logger)
        {
            _logger = logger;
        }

        public async Task ProcessMessageAsync(
            OrderV2 message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace("Write order v2 message to disk: {MessageId}", message.Id);

            string fileName = message.Id + ".json";
            string dirPath = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(dirPath, fileName);

            string json = JsonConvert.SerializeObject(
                new OrderCreatedEventData(
                    message.Id,
                    message.Amount,
                    message.ArticleNumber,
                    message.Customer.FirstName + " " + message.Customer.LastName,
                    correlationInfo));

            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }
    }
}