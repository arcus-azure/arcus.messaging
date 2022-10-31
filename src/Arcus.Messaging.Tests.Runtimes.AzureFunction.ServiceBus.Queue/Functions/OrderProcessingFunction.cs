using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Queue.Functions
{
    public class OrderProcessingFunction
    {
        private readonly IAzureServiceBusMessageRouter _messageRouter;
        private readonly TelemetryClient _telemetryClient;
        private readonly string _jobId;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderProcessingFunction" /> class.
        /// </summary>
        public OrderProcessingFunction(
            IAzureServiceBusMessageRouter messageRouter,
            TelemetryClient telemetryClient)
        {
            _messageRouter = messageRouter;
            _telemetryClient = telemetryClient;
            _jobId = Guid.NewGuid().ToString();
        }

        [FunctionName("order-processing")]
        public async Task Run(
            [ServiceBusTrigger("docker-az-func-queue", Connection = "ARCUS_SERVICEBUS_CONNECTIONSTRING")] ServiceBusReceivedMessage message,
            ILogger log,
            CancellationToken cancellationToken)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {message.MessageId}");

            AzureServiceBusMessageContext context = message.GetMessageContext(_jobId);
            (string transactionId, string operationParentId) = message.ApplicationProperties.GetTraceParent();

            using (var result = MessageCorrelationResult.Create(_telemetryClient, transactionId, operationParentId))
            {
                await _messageRouter.RouteMessageAsync(message, context, result.CorrelationInfo, cancellationToken);
            }
        }
    }
}
