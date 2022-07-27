using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrderEventHubsFallbackMessageHandler : IFallbackMessageHandler
    {
        private readonly IEventGridPublisher _eventGridPublisher;
        private readonly ILogger<OrderEventHubsMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderEventHubsFallbackMessageHandler" /> class.
        /// </summary>
        public OrderEventHubsFallbackMessageHandler(
            IEventGridPublisher eventGridPublisher,
            ILogger<OrderEventHubsMessageHandler> logger)
        {
            _eventGridPublisher = eventGridPublisher;
            _logger = logger;
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="json">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">
        ///     The information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers.
        /// </param>
        /// <param name="cancellationToken">The cancellation token to cancel the processing.</param>
        public async Task ProcessMessageAsync(
            string json,
            MessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var message = JsonConvert.DeserializeObject<Order>(json);
            await _eventGridPublisher.PublishOrderAsync(message, correlationInfo, _logger);
        }
    }
}
