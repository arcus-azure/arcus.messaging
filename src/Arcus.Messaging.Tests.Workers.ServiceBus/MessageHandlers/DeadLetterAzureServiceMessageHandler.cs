using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class DeadLetterAzureServiceMessageHandler : IAzureServiceBusMessageHandler<Order>
    {
        private readonly ILogger<DeadLetterAzureServiceMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageHandlerTemplate"/> class.
        /// </summary>
        public DeadLetterAzureServiceMessageHandler(ILogger<DeadLetterAzureServiceMessageHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ProcessMessageAsync(
            Order message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace("Dead letter message '{OrderId}'...", message.Id);
            await messageContext.DeadLetterMessageAsync("Test dead-letter reason", "dead-lettered by test", cancellationToken);
            _logger.LogInformation("Message '{OrderId}' is dead lettered", message.Id);
        }
    }
}
