using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using Arcus.Messaging.Tests.Contracts.Events.v1;
using Arcus.Messaging.Tests.Contracts.Messages.v1;
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
        public async Task MessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed(Encoding messageEncoding)
        {
            // Arrange
            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();
            var messageSender = CreateServiceBusSender();

            var order = GenerateOrder();
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

        private MessageSender CreateServiceBusSender()
        {
            var connectionString = Configuration.GetValue<string>("Arcus:ServiceBus:ConnectionStringWithQueue");
            var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            var messageSender = new MessageSender(serviceBusConnectionStringBuilder.GetNamespaceConnectionString(), serviceBusConnectionStringBuilder.EntityPath);
            return messageSender;
        }

        public async Task InitializeAsync()
        {
            var connectionString = Configuration.GetValue<string>("Arcus:Infra:ConnectionString");
            var topicName = Configuration.GetValue<string>("Arcus:Infra:TopicName");

            var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);
            _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, Logger);
        }

        public async Task DisposeAsync()
        {
            await _serviceBusEventConsumerHost.StopAsync();
        }

        private static Order GenerateOrder()
        {
            var customerGenerator = new Faker<Customer>()
                .RuleFor(u => u.FirstName, (f, u) => f.Name.FirstName())
                .RuleFor(u => u.LastName, (f, u) => f.Name.LastName());

            var orderGenerator = new Faker<Order>()
                .RuleFor(u => u.Customer, () => customerGenerator)
                .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
                .RuleFor(u => u.Amount, f => f.Random.Int())
                .RuleFor(u => u.ArticleNumber, f => f.Commerce.Product());

            return orderGenerator.Generate();
        }
    }
}