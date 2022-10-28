using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrderV2AzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<OrderV2>
    {
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly ILogger<OrderV2AzureServiceBusMessageHandler> _logger;

        public OrderV2AzureServiceBusMessageHandler(IEventGridPublisher eventGridPublisher, ILogger<OrderV2AzureServiceBusMessageHandler> logger)
        {
            Guard.NotNull(eventGridPublisher, nameof(eventGridPublisher));
            Guard.NotNull(logger, nameof(logger));

            _eventGridPublisher = eventGridPublisher;
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
            OrderV2 order,
            AzureServiceBusMessageContext azureMessageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            await _eventGridPublisher.PublishOrderAsync(order, correlationInfo, _logger);
        }
    }
}
