using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Health;
using Bogus;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Trait("Category", "Integration")]
    public class ServiceBusMessagePumpTests : IntegrationTest, IAsyncLifetime
    {
        private const string QueueConnectionStringKey = "Arcus:ServiceBus:ConnectionStringWithQueue";
        private const string TopicConnectionStringKey = "Arcus:ServiceBus:ConnectionStringWithTopic";

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

        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpHealthCheckTests" /> class.
        /// </summary>
        public ServiceBusMessagePumpTests(ITestOutputHelper testOutput) : base(testOutput)
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
            var messageSender = CreateServiceBusSender(connectionStringKey);

            var order = OrderGenerator.Generate();
            var orderMessage = order.WrapInServiceBusMessage(operationId, transactionId, encoding: messageEncoding);

            // Act
            await messageSender.SendAsync(orderMessage);

            // Assert
            var receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(operationId);
            Assert.NotEmpty(receivedEvent);
            var deserializedEventGridMessage = EventGridParser.Parse<OrderCreatedEvent>(receivedEvent);
            Assert.NotNull(deserializedEventGridMessage);
            Assert.Single(deserializedEventGridMessage.Events);
            var orderCreatedEvent = deserializedEventGridMessage.Events.SingleOrDefault();
            var orderCreatedEventData = orderCreatedEvent.GetPayload<OrderCreatedEventData>();
            Assert.NotNull(orderCreatedEventData);
            Assert.NotNull(orderCreatedEventData.CorrelationInfo);
            Assert.Equal(order.Id, orderCreatedEventData.Id);
            Assert.Equal(order.Amount, orderCreatedEventData.Amount);
            Assert.Equal(order.ArticleNumber, orderCreatedEventData.ArticleNumber);
            Assert.Equal(transactionId, orderCreatedEventData.CorrelationInfo.TransactionId);
            Assert.Equal(operationId, orderCreatedEventData.CorrelationInfo.OperationId);
            Assert.NotEmpty(orderCreatedEventData.CorrelationInfo.CycleId);
        }

        private MessageSender CreateServiceBusSender(string connectionStringKey)
        {
            var connectionString = Configuration.GetValue<string>(connectionStringKey);
            var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            var messageSender = new MessageSender(serviceBusConnectionStringBuilder);
            return messageSender;
        }

        public async Task InitializeAsync()
        {
            var connectionString = Configuration.GetValue<string>("Arcus:Infra:ServiceBus:ConnectionString");
            var topicName = Configuration.GetValue<string>("Arcus:Infra:ServiceBus:TopicName");

            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);
            _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, Logger);
        }

        public async Task DisposeAsync()
        {
            await _serviceBusEventConsumerHost.StopAsync();
        }
    }
}