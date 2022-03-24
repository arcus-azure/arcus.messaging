using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Azure.Messaging.ServiceBus;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Microsoft.Azure.ServiceBus;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Docker")]
    [Trait("Category", "Docker")]
    public class ServiceBusMessagePumpDockerTests : DockerServiceBusIntegrationTest
    {
        private const string QueueConnectionStringKey = "Arcus:ServiceBus:Docker:ConnectionStringWithQueue";
        private const string TopicConnectionStringKey = "Arcus:ServiceBus:Docker:ConnectionStringWithTopic";

        public static IEnumerable<object[]> Encodings
        {
            get
            {
                yield return new object[] { Encoding.UTF8 };
                //yield return new object[] { Encoding.UTF7 };
                //yield return new object[] { Encoding.UTF32 };
                //yield return new object[] { Encoding.ASCII };
                //yield return new object[] { Encoding.Unicode };
                //yield return new object[] { Encoding.BigEndianUnicode };
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessagePumpDockerTests" /> class.
        /// </summary>
        public ServiceBusMessagePumpDockerTests(ITestOutputHelper testOutput) : base(testOutput)
        {
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task ServiceBusQueueMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed(Encoding messageEncoding)
        {
            await ServiceBusMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed(messageEncoding, QueueConnectionStringKey);
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task ServiceBusTopicMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed(Encoding messageEncoding)
        {
            await ServiceBusMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed(messageEncoding, TopicConnectionStringKey);
        }

        private async Task ServiceBusMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed(Encoding messageEncoding, string connectionStringKey)
        {
            // Arrange
            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            Order order = OrderGenerator.Generate();
            ServiceBusMessage orderMessage = order.AsServiceBusMessage(operationId, transactionId, encoding: messageEncoding);

            // Act
            await SenderOrderToServiceBusAsync(orderMessage, connectionStringKey);

            // Assert
            OrderCreatedEventData orderCreatedEventData = ReceiveOrderFromEventGrid(operationId);
            Assert.NotNull(orderCreatedEventData);
            Assert.NotNull(orderCreatedEventData.CorrelationInfo);
            Assert.Equal(order.Id, orderCreatedEventData.Id);
            Assert.Equal(order.Amount, orderCreatedEventData.Amount);
            Assert.Equal(order.ArticleNumber, orderCreatedEventData.ArticleNumber);
            Assert.Equal(transactionId, orderCreatedEventData.CorrelationInfo.TransactionId);
            Assert.Equal(operationId, orderCreatedEventData.CorrelationInfo.OperationId);
            Assert.NotEmpty(orderCreatedEventData.CorrelationInfo.CycleId);
        }
    }
}