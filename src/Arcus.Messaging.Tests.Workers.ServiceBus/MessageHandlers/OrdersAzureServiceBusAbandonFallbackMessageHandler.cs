using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersAzureServiceBusAbandonFallbackMessageHandler : AzureServiceBusFallbackMessageHandler
    {
        private readonly EventGridPublisherClient _eventGridPublisher;

        public OrdersAzureServiceBusAbandonFallbackMessageHandler(
            IAzureClientFactory<EventGridPublisherClient> clientFactory, 
            ILogger<OrdersAzureServiceBusMessageHandler> logger)
            : base(logger)
        {
            Guard.NotNull(clientFactory, nameof(clientFactory));

            _eventGridPublisher = clientFactory.CreateClient("Default");
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="azureMessageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public override async Task ProcessMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext azureMessageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            if (azureMessageContext.SystemProperties.DeliveryCount <= 1)
            {
                Logger.LogTrace("Abandoning message '{MessageId}'...", message.MessageId);
                await AbandonMessageAsync(message);
                Logger.LogTrace("Abandoned message '{MessageId}'", message.MessageId);
            }
            else
            {
                string json = Encoding.UTF8.GetString(message.Body);
                var order = JsonConvert.DeserializeObject<Order>(json);
                await _eventGridPublisher.PublishOrderAsync(order, correlationInfo, Logger);
            }
        }
    }
}
