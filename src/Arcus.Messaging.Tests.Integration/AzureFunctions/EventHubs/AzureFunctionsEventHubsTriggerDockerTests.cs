using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.EventHubs;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Azure.Messaging.EventHubs;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.AzureFunctions.EventHubs
{
    [Collection("Docker")]
    [Trait("Category", "Docker")]
    public class AzureFunctionsEventHubsTriggerDockerTests : DockerServiceBusIntegrationTest
    {
        private readonly TestConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsEventHubsTriggerDockerTests" /> class.
        /// </summary>
        public AzureFunctionsEventHubsTriggerDockerTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
            _config = TestConfig.Create();
        }

        [Theory]
        [InlineData(IntegrationTestType.DockerAzureFunctionsInProcess)]
        [InlineData(IntegrationTestType.DockerAzureFunctionsIsolated)]
        public async Task EventHubsMessageFunction_PublishesEventDataMessage_MessageSuccessfullyProcessed(IntegrationTestType integrationType)
        {
            // Arrange
            var traceParent = TraceParent.Generate();
            Order order = OrderGenerator.Generate();
            EventData expected = new EventData(JsonConvert.SerializeObject(order)).WithDiagnosticId(traceParent);

            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            string eventHubsName = eventHubs.GetEventHubsName(integrationType);
            var producer = new TestEventHubsMessageProducer(eventHubs.EventHubsConnectionString, eventHubsName);

            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, Logger))
            {
                // Act
                await producer.ProduceAsync(expected);

                // Assert
                OrderCreatedEventData orderCreatedEventData = consumer.ConsumeOrderEventForW3C(traceParent.TransactionId);
                Assert.NotNull(orderCreatedEventData);
                Assert.NotNull(orderCreatedEventData.CorrelationInfo);
                Assert.Equal(order.Id, orderCreatedEventData.Id);
                Assert.Equal(order.Amount, orderCreatedEventData.Amount);
                Assert.Equal(order.ArticleNumber, orderCreatedEventData.ArticleNumber);
                Assert.Equal(traceParent.TransactionId, orderCreatedEventData.CorrelationInfo.TransactionId);
                Assert.Equal(traceParent.OperationParentId, orderCreatedEventData.CorrelationInfo.OperationParentId);
                Assert.NotNull(orderCreatedEventData.CorrelationInfo.OperationId);
            }
        }
    }
}
