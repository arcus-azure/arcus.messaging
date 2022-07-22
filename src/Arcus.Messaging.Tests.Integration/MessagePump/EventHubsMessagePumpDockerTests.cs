using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.EventHubs;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Testing.Logging;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Docker")]
    [Trait("Category", "Docker")]
    public class EventHubsMessagePumpDockerTests : DockerServiceBusIntegrationTest
    {
        private readonly TestConfig _config;

        public EventHubsMessagePumpDockerTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
            _config = TestConfig.Create();
        }

        [Fact]
        public async Task EventHubsMessagePump_PublishEventDataMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var operationId = $"operation-eventhubs-{Guid.NewGuid()}";
            var transactionId = $"transaction-{Guid.NewGuid()}";
            Order order = OrderGenerator.Generate();
            EventData expected = CreateOrderEventDataMessage(order, operationId, transactionId);

            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            string eventHubsName = eventHubs.GetEventHubsName(IntegrationTestType.Docker);
            var producer = new TestEventHubsMessageProducer(eventHubs.EventHubsConnectionString, eventHubsName);

            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, Logger))
            {
                // Act
                await producer.ProduceAsync(expected);

                // Assert
                OrderCreatedEventData orderCreatedEventData = consumer.ConsumeOrderEvent(expected.CorrelationId);
                Assert.NotNull(orderCreatedEventData);
                Assert.NotNull(orderCreatedEventData.CorrelationInfo);
                Assert.Equal(order.Id, orderCreatedEventData.Id);
                Assert.Equal(order.Amount, orderCreatedEventData.Amount);
                Assert.Equal(order.ArticleNumber, orderCreatedEventData.ArticleNumber);
                Assert.Equal(transactionId, orderCreatedEventData.CorrelationInfo.TransactionId);
                Assert.Equal(operationId, orderCreatedEventData.CorrelationInfo.OperationId);
            }
        }

        private static EventData CreateOrderEventDataMessage(Order order, string operationId, string transactionId)
        {
            var eventData = new EventData(JsonConvert.SerializeObject(order));
            eventData.MessageId = $"message-{Guid.NewGuid()}";
            eventData.CorrelationId = operationId;
            eventData.Properties[PropertyNames.TransactionId] = transactionId;
            eventData.Properties[PropertyNames.OperationParentId] = $"parent-{Guid.NewGuid()}";

            return eventData;
        }
    }
}
