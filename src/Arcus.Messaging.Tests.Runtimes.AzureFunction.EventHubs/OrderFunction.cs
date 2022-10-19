using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Azure.Messaging.EventHubs;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Runtimes.AzureFunction.EventHubs
{
    public class OrderFunction
    {
        private readonly IAzureEventHubsMessageRouter _messageRouter;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger _logger;

        public OrderFunction(
            IAzureEventHubsMessageRouter messageRouter,
            TelemetryClient telemetryClient,
            ILoggerFactory loggerFactory)
        {
            _messageRouter = messageRouter;
            _telemetryClient = telemetryClient;
            _logger = loggerFactory.CreateLogger<OrderFunction>();
        }

        [Function("orders")]
        public async Task Run(
            [EventHubTrigger("orders-az-func-docker", Connection = "EventHubsConnectionString")] string[] messages,
            Dictionary<string, JsonElement>[] propertiesArray)
        {
            _logger.LogInformation($"First Event Hubs triggered message: {messages[0]}");

            for (int i = 0; i < messages.Length; i++)
            {
                string message = messages[i];
                IDictionary<string, object> properties = GetUserProperties(propertiesArray, i);
                EventData eventData = CreateEventData(message, properties);
                var context = AzureEventHubsMessageContext.CreateFrom(eventData, "<namespace>", "$Default", "<eventhubs-name>");

                (string transactionId, string operationParentId) = properties.GetTraceParent();
                using (var result = MessageCorrelationResult.Create(_telemetryClient, transactionId, operationParentId))
                {
                    await _messageRouter.RouteMessageAsync(eventData, context, result.CorrelationInfo, CancellationToken.None);
                }
            }
        }

        private static Dictionary<string, object> GetUserProperties(Dictionary<string, JsonElement>[] propertiesArray, int i)
        {
            return propertiesArray[i].ToDictionary(item => item.Key, item => (object) item.Value.GetString());
        }

        private static EventData CreateEventData(string message, IDictionary<string, object> properties)
        {
            var data = new EventData(message);
            foreach (KeyValuePair<string, object> property in properties)
            {
                data.Properties.Add(property.Key, property.Value.ToString());
            }

            return data;
        }
    }
}
