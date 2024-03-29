﻿using System.Text;
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
    public class OrdersServiceBusFallbackMessageHandler : IAzureServiceBusFallbackMessageHandler
    {
        private readonly EventGridPublisherClient _eventGridPublisher;
        private readonly ILogger<OrdersAzureServiceBusMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersAzureServiceBusMessageHandler"/> class.
        /// </summary>
        /// <param name="clientFactory">The publisher instance to send event messages to Azure Event Grid.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the interaction with Azure Event Grid.</param>
        public OrdersServiceBusFallbackMessageHandler(
            IAzureClientFactory<EventGridPublisherClient> clientFactory, 
            ILogger<OrdersAzureServiceBusMessageHandler> logger)
        {
            Guard.NotNull(clientFactory, nameof(clientFactory));
            Guard.NotNull(logger, nameof(logger));

            _eventGridPublisher = clientFactory.CreateClient("Default");
            _logger = logger;
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
        public async Task ProcessMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext azureMessageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            string messageBody = Encoding.UTF8.GetString(message.Body);
            var order = JsonConvert.DeserializeObject<Order>(messageBody);

            await _eventGridPublisher.PublishOrderAsync(order, correlationInfo, _logger);
        }
    }
}
