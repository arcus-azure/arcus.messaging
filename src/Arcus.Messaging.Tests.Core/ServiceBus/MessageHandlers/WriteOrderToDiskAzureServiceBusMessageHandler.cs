using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class WriteOrderToDiskAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        private readonly ILogger<WriteOrderToDiskAzureServiceBusMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteOrderToDiskAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public WriteOrderToDiskAzureServiceBusMessageHandler(
            ILogger<WriteOrderToDiskAzureServiceBusMessageHandler> logger)
        {
            _logger = logger;
        }

        public async Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace("[Test] Write order v1 message to disk: {MessageId}", message.Id);

            string fileName = message.Id + ".json";
            string dirPath = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(dirPath, fileName);

            string json = JsonConvert.SerializeObject(
                new OrderCreatedEventData
                {
                    Id = message.Id,
                    Amount = message.Amount,
                    ArticleNumber = message.ArticleNumber,
                    CustomerName = message.Customer.FirstName + " " + message.Customer.LastName,
                    CorrelationInfo = new()
                    {
                        OperationId = correlationInfo.OperationId,
                        TransactionId = correlationInfo.TransactionId,
                        OperationParentId = correlationInfo.OperationParentId,
                    },
                    MessageContext = new()
                    {
                        EntityName = messageContext.EntityPath,
                        SubscriptionName = messageContext.SubscriptionName
                    }
                });

            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }
    }
}