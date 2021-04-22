using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
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
                yield return new object[] { Encoding.UTF7 };
                yield return new object[] { Encoding.UTF32 };
                yield return new object[] { Encoding.ASCII };
                yield return new object[] { Encoding.Unicode };
                yield return new object[] { Encoding.BigEndianUnicode };
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

        private async Task ServiceBusMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed(Encoding messageEncoding, string connectionString)
        {
            // Arrange
            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            
            Order order = OrderGenerator.Generate();
            Message orderMessage = order.AsServiceBusMessage(operationId, transactionId, encoding: messageEncoding);
            
            // Act
            await SenderOrderToServiceBusAsync(orderMessage, connectionString);
            
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