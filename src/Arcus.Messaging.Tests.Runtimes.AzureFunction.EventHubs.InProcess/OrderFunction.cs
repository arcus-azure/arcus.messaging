using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.AzureFunctions.EventHubs;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs.InProcess
{
    public class OrderFunction
    {
        private readonly string _jobId = Guid.NewGuid().ToString();
        private readonly IAzureEventHubsMessageRouter _messageRouter;
        private readonly AzureFunctionsInProcessMessageCorrelation _messageCorrelation;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFunction" /> class.
        /// </summary>
        public OrderFunction(
            IAzureEventHubsMessageRouter messageRouter,
            AzureFunctionsInProcessMessageCorrelation messageCorrelation)
        {
            _messageRouter = messageRouter;
            _messageCorrelation = messageCorrelation;
        }

        [FunctionName("orders")]
        public async Task Run(
            [EventHubTrigger("orders-az-func-inprocess-docker", Connection = "EventHubsConnectionString")] EventData[] events, 
            ILogger log,
            CancellationToken cancellation)
        {
            log.LogInformation("Processing first Azure EventHubs message: {MessageId}", events[0].MessageId);

            foreach (EventData eventData in events)
            {
                AzureEventHubsMessageContext messageContext = eventData.GetMessageContext("<eventhubs-namespace>", "<eventhubs-name>", "$Default", _jobId);
                using (MessageCorrelationResult result = _messageCorrelation.CorrelateMessage(eventData))
                {
                    await _messageRouter.RouteMessageAsync(eventData, messageContext, result.CorrelationInfo, cancellation);
                }
            }
        }
    }
}
