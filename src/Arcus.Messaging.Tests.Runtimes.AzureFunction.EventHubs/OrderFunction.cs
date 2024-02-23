using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs
{
    public class OrderFunction
    {
        private readonly IAzureEventHubsMessageRouter _messageRouter;
        private readonly string _jobId = Guid.NewGuid().ToString();
        private readonly ILogger _logger;

        public OrderFunction(
            IAzureEventHubsMessageRouter messageRouter,
            ILoggerFactory loggerFactory)
        {
            _messageRouter = messageRouter;
            _logger = loggerFactory.CreateLogger<OrderFunction>();
        }

        [Function("orders")]
        public async Task Run(
            [EventHubTrigger("orders-az-func-docker", Connection = "EventHubsConnectionString")] string[] messages,
            Dictionary<string, JsonElement>[] propertiesArray,
            FunctionContext executionContext)
        {
            _logger.LogInformation($"First Event Hubs triggered message: {messages[0]}");

            for (var i = 0; i < messages.Length; i++)
            {
                string message = messages[i];
                Dictionary<string, JsonElement> properties = propertiesArray[i];
                
                EventData eventData = CreateEventData(message, properties);
                AzureEventHubsMessageContext messageContext = eventData.GetMessageContext("<namespace>", "$Default", "<eventhubs-name>", _jobId);

                using (MessageCorrelationResult result = executionContext.GetCorrelationInfo(properties))
                {
                    await _messageRouter.RouteMessageAsync(eventData, messageContext, result.CorrelationInfo, CancellationToken.None);
                }
            }
        }

        private static EventData CreateEventData(string message, IDictionary<string, JsonElement> properties)
        {
            var data = new EventData(message);
            foreach (KeyValuePair<string, JsonElement> property in properties)
            {
                data.Properties.Add(property.Key, property.Value.ToString());
            }

            return data;
        }
    }
}
