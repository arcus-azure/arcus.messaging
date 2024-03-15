using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Azure.Messaging.ServiceBus;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.AzureFunctions.ServiceBus
{
    [Collection("Docker")]
    [Trait("Category", "Docker")]
    public class AzureFunctionServiceBusTriggerDockerTests : DockerServiceBusIntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionServiceBusTriggerDockerTests" /> class.
        /// </summary>
        public AzureFunctionServiceBusTriggerDockerTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Theory]
        [InlineData("Arcus:ServiceBus:Docker:AzureFunctions:ConnectionStringWithQueue", Skip = ".NET 8 is not supported yet for in-process Azure Functions")]
        [InlineData("Arcus:ServiceBus:Docker:AzureFunctions:ConnectionStringWithTopic")]
        public async Task ServiceBusTrigger_PublishServiceBusMessage_MessageSuccessfullyProcessed(string connectionStringKey)
        {
            // Arrange
            var traceParent = TraceParent.Generate();
            Order order = OrderGenerator.Generate();
            var orderMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(order)).WithDiagnosticId(traceParent);

            // Act
            await SenderOrderToServiceBusAsync(orderMessage, connectionStringKey);
            
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
