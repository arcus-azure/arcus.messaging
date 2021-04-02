using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class OrdersAzureServiceBusDeadLetterFallbackMessageHandler : AzureServiceBusFallbackMessageHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusFallbackMessageHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger instance to write diagnostic messages during the message handling.</param>
        public OrdersAzureServiceBusDeadLetterFallbackMessageHandler(
            ILogger<OrdersAzureServiceBusDeadLetterFallbackMessageHandler> logger)
            : base(logger)
        {
        }

        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">The Azure Service Bus Message message that was received</param>
        /// <param name="messageContext">The context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     The information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers.
        /// </param>
        /// <param name="cancellationToken">The cancellation token to cancel the processing.</param>
        public override async Task ProcessMessageAsync(
            Message message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace("Dead letter message '{MessageId}'...", message.MessageId);
            await DeadLetterAsync(message);
            Logger.LogInformation("Message '{MessageId}' is dead lettered", message.MessageId);
        }
    }
}
