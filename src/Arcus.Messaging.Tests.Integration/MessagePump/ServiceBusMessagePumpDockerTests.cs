using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Azure.Messaging.ServiceBus;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Bogus;
using Microsoft.Azure.ServiceBus;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Docker")]
    [Trait("Category", "Docker")]
    public class ServiceBusMessagePumpDockerTests : DockerServiceBusIntegrationTest
    {
        private static readonly Faker BogusGenerator = new Faker();

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
            var traceParent = TraceParent.Generate();
            Order order = OrderGenerator.Generate();
            var orderMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(order))
                .WithDiagnosticId(traceParent);

            // Act
            await SenderOrderToServiceBusAsync(orderMessage, connectionString);

            // Assert
            OrderCreatedEventData orderCreatedEventData = ReceiveOrderFromEventGrid(traceParent.TransactionId);
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