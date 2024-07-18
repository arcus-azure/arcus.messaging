using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.ServiceBus.Fixture;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class WriteOrderToDiskFallbackMessageHandler : AzureServiceBusFallbackMessageHandler, IFallbackMessageHandler
    {
        private readonly ILogger<WriteOrderToDiskFallbackMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteOrderToDiskFallbackMessageHandler" /> class.
        /// </summary>
        public WriteOrderToDiskFallbackMessageHandler(
            ILogger<WriteOrderToDiskFallbackMessageHandler> logger) :
            base(logger)
        {
            _logger = logger;
        }

        public override async Task ProcessMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var order = message.Body.ToObjectFromJson<Order>();
            await ProcessMessageAsync(order, correlationInfo, cancellationToken);
        }

        public async Task ProcessMessageAsync(
            string message,
            MessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var order = JsonConvert.DeserializeObject<Order>(message, new MessageCorrelationInfoJsonConverter());
            await ProcessMessageAsync(order, correlationInfo, cancellationToken);
        }

        private async Task ProcessMessageAsync(
            Order order,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace("Write order v1 to disk: {MessageId}", order.Id);

            string fileName = order.Id + ".json";
            string dirPath = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(dirPath, fileName);

            string json = JsonConvert.SerializeObject(
                new OrderCreatedEventData(
                    order.Id,
                    order.Amount,
                    order.ArticleNumber,
                    order.Customer.FirstName + " " + order.Customer.LastName,
                    correlationInfo));

            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }
    }
}