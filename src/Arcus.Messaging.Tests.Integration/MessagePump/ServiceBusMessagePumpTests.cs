﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.ServiceBus.Core;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using Arcus.Messaging.Tests.Contracts.Events.v1;
using Arcus.Messaging.Tests.Contracts.Messages.v1;
using Arcus.Messaging.Tests.Integration.Health;
using Bogus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
                yield return new object[] { Encoding.UTF32};
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

            var orderMessage = GenerateOrder().AsServiceBusMessage(encoding: messageEncoding);
            orderMessage.CorrelationId = operationId;
            orderMessage.UserProperties.Add(PropertyNames.TransactionId, transactionId);

            // Act
            await messageSender.SendAsync(orderMessage);

            // Assert
            var receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(operationId); Assert.NotEqual(String.Empty, receivedEvent);

            var deserializedEventGridMessage = EventGridParser.Parse<OrderCreatedEvent>(receivedEvent);
            Assert.NotNull(deserializedEventGridMessage);
        }

        private MessageSender CreateServiceBusSender()
        {
            var connectionString = Configuration.GetValue<string>("Arcus:ServiceBus:ConnectionString");
            var messageSender = new MessageSender(connectionString, "orders");
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