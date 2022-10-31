using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Azure.Messaging.ServiceBus;
using Bogus;
using Microsoft.Azure.ServiceBus;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.AzureFunctions.ServiceBus
{
    [Collection("Docker")]
    [Trait("Category", "Docker")]
    public class AzureFunctionServiceBusQueueTriggerDockerTests : DockerServiceBusIntegrationTest
    {
        private const string QueueConnectionString = "Arcus:ServiceBus:Docker:AzureFunctions:ConnectionStringWithQueue";

        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionServiceBusQueueTriggerDockerTests" /> class.
        /// </summary>
        public AzureFunctionServiceBusQueueTriggerDockerTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task ServiceBusQueueTrigger_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var traceParent = TraceParent.Generate();
            Order order = OrderGenerator.Generate();
            var orderMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(order)).WithDiagnosticId(traceParent);

            // Act
            await SenderOrderToServiceBusAsync(orderMessage, QueueConnectionString);
            
            // Assert
            OrderCreatedEventData orderEventData = ReceiveOrderFromEventGrid(traceParent.TransactionId);
            Assert.NotNull(orderEventData.CorrelationInfo);
            Assert.Equal(order.Id, orderEventData.Id);
            Assert.Equal(order.Amount, orderEventData.Amount);
            Assert.Equal(order.ArticleNumber, orderEventData.ArticleNumber);
            Assert.Equal(traceParent.TransactionId, orderEventData.CorrelationInfo.TransactionId);
            Assert.Equal(traceParent.OperationParentId, orderEventData.CorrelationInfo.OperationParentId);
            Assert.NotNull(orderEventData.CorrelationInfo.OperationId);
        }
    }
}
