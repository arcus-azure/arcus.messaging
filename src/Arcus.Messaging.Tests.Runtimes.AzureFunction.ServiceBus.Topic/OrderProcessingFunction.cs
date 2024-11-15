using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Runtimes.AzureFunction.ServiceBus.Topic
{
    public class OrderProcessingFunction
    {
        private readonly IAzureServiceBusMessageRouter _messageRouter;
        private readonly string _jobId;
        private readonly ILogger _logger;

        public OrderProcessingFunction(
            IAzureServiceBusMessageRouter messageRouter,
            ILoggerFactory loggerFactory)
        {
            _messageRouter = messageRouter;
            _jobId = Guid.NewGuid().ToString();
            _logger = loggerFactory.CreateLogger<OrderProcessingFunction>();
        }

        [Function("order-processing")]
        public async Task Run(
            [ServiceBusTrigger("docker-az-func-topic", "TestSubscription", Connection = "ARCUS_SERVICEBUS_CONNECTIONSTRING")] ServiceBusReceivedMessage message,
            FunctionContext executionContext)
        {
            _logger.LogInformation("C# ServiceBus topic trigger function processed message: {MessageId}", message.MessageId);

            AzureServiceBusMessageContext messageContext = message.GetMessageContext(_jobId);
            using (MessageCorrelationResult result = executionContext.GetCorrelationInfo())
            {
                await _messageRouter.RouteMessageAsync(message, messageContext, result.CorrelationInfo, CancellationToken.None);
            }
        }
    }
}
