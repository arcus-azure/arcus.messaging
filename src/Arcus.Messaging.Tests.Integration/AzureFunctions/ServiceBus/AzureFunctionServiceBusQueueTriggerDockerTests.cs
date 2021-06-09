using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Azure.Messaging.ServiceBus;
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
            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            
            Order order = OrderGenerator.Generate();
            ServiceBusMessage orderMessage = order.AsServiceBusMessage(operationId, transactionId);

            // Act
            await SenderOrderToServiceBusAsync(orderMessage, QueueConnectionString);
            
            // Assert
            OrderCreatedEventData orderEventData = ReceiveOrderFromEventGrid(operationId);
            Assert.NotNull(orderEventData.CorrelationInfo);
            Assert.Equal(order.Id, orderEventData.Id);
            Assert.Equal(order.Amount, orderEventData.Amount);
            Assert.Equal(order.ArticleNumber, orderEventData.ArticleNumber);
            Assert.Equal(transactionId, orderEventData.CorrelationInfo.TransactionId);
            Assert.Equal(operationId, orderEventData.CorrelationInfo.OperationId);
            Assert.NotEmpty(orderEventData.CorrelationInfo.CycleId);
        }
    }
}
