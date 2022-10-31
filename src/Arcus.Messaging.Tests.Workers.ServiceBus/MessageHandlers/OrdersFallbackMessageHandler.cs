using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using GuardNet;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersFallbackMessageHandler : IFallbackMessageHandler
    {
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly ILogger<OrdersAzureServiceBusMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersAzureServiceBusMessageHandler"/> class.
        /// </summary>
        /// <param name="eventGridPublisher">The publisher instance to send event messages to Azure Event Grid.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the interaction with Azure Event Grid.</param>
        public OrdersFallbackMessageHandler(IEventGridPublisher eventGridPublisher, ILogger<OrdersAzureServiceBusMessageHandler> logger)
        {
            Guard.NotNull(eventGridPublisher, nameof(eventGridPublisher));
            Guard.NotNull(logger, nameof(logger));

            _eventGridPublisher = eventGridPublisher;
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
            string message,
            MessageContext azureMessageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var order = JsonConvert.DeserializeObject<Order>(message);

            await _eventGridPublisher.PublishOrderAsync(order, correlationInfo, _logger);
        }
    }
}
