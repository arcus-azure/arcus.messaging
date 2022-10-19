using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
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
            string transactionId = BogusGenerator.Random.Hexadecimal(32, prefix: null);
            string operationParentId = BogusGenerator.Random.Hexadecimal(16, prefix: null);
            
            Order order = OrderGenerator.Generate();
            var orderMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(order));
            orderMessage.ApplicationProperties["Diagnostic-Id"] = $"00-{transactionId}-{operationParentId}-00";

            // Act
            await SenderOrderToServiceBusAsync(orderMessage, QueueConnectionString);
            
            // Assert
            OrderCreatedEventData orderEventData = ReceiveOrderFromEventGrid(transactionId);
            Assert.NotNull(orderEventData.CorrelationInfo);
            Assert.Equal(order.Id, orderEventData.Id);
            Assert.Equal(order.Amount, orderEventData.Amount);
            Assert.Equal(order.ArticleNumber, orderEventData.ArticleNumber);
            Assert.Equal(transactionId, orderEventData.CorrelationInfo.TransactionId);
            Assert.NotNull(orderEventData.CorrelationInfo.OperationId);
        }
    }
}
