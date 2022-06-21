using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Azure.Messaging.ServiceBus;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Docker")]
    [Trait("Category", "Docker")]
    public class ServiceBusMessagePumpDockerTests : DockerServiceBusIntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessagePumpDockerTests" /> class.
        /// </summary>
        public ServiceBusMessagePumpDockerTests(ITestOutputHelper testOutput) : base(testOutput)
        {
        }

        [Theory]
        [InlineData("Arcus:ServiceBus:Docker:ConnectionStringWithQueue")]
        [InlineData("Arcus:ServiceBus:Docker:ConnectionStringWithTopic")]
        public async Task ServiceBusTopicMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed(string connectionString)
        {
            // Arrange
            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            Order order = OrderGenerator.Generate();

            ServiceBusMessageBuilder serviceBusMessageBuilder = ServiceBusMessageBuilder.CreateForBody(order);
            serviceBusMessageBuilder.WithOperationId(operationId);
            serviceBusMessageBuilder.WithTransactionId(transactionId);

            ServiceBusMessage orderMessage = serviceBusMessageBuilder.Build();

            // Act
            await SenderOrderToServiceBusAsync(orderMessage, connectionString);

            // Assert
            OrderCreatedEventData orderCreatedEventData = ReceiveOrderFromEventGrid(operationId);
            Assert.NotNull(orderCreatedEventData);
            Assert.NotNull(orderCreatedEventData.CorrelationInfo);
            Assert.Equal(order.Id, orderCreatedEventData.Id);
            Assert.Equal(order.Amount, orderCreatedEventData.Amount);
            Assert.Equal(order.ArticleNumber, orderCreatedEventData.ArticleNumber);
            Assert.Equal(transactionId, orderCreatedEventData.CorrelationInfo.TransactionId);
            Assert.Equal(operationId, orderCreatedEventData.CorrelationInfo.OperationId);
        }
    }
}