using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.EventGrid;
using GuardNet;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        private readonly EventGridPublisherClient _eventGridPublisher;
        private readonly IMessageCorrelationInfoAccessor _correlationAccessor;
        private readonly ILogger<OrdersAzureServiceBusMessageHandler> _logger;

        public OrdersAzureServiceBusMessageHandler(
            IAzureClientFactory<EventGridPublisherClient> clientFactory, 
            IMessageCorrelationInfoAccessor correlationAccessor,
            ILogger<OrdersAzureServiceBusMessageHandler> logger)
        {
            Guard.NotNull(clientFactory, nameof(clientFactory));
            Guard.NotNull(logger, nameof(logger));

            _eventGridPublisher = clientFactory.CreateClient("Default");
            _correlationAccessor = correlationAccessor;
            _logger = logger;
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="order">Message that was received</param>
        /// <param name="azureMessageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ProcessMessageAsync(
            Order order,
            AzureServiceBusMessageContext azureMessageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            EnsureSameCorrelation(correlationInfo);
            await _eventGridPublisher.PublishOrderAsync(order, correlationInfo, _logger);
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
