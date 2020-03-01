using System;
using System.Threading.Tasks;
using Arcus.EventGrid;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Logging;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using GuardNet;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    /// <summary>
    /// Represents a service to interact with the hosted-service.
    /// </summary>
    public class TestMessagePumpService : IAsyncDisposable
    {
        private readonly ServiceBusEntity _entity;
        private readonly ITestOutputHelper _outputWriter;
        private readonly TestConfig _configuration;

        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        private TestMessagePumpService(ServiceBusEntity entity, TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            _entity = entity;
            _outputWriter = outputWriter;
            _configuration = configuration;
        }

        /// <summary>
        /// Starts a new instance of the <see cref="TestMessagePumpService"/> type to simulate messages.
        /// </summary>
        public static async Task<TestMessagePumpService> StartNewAsync(
            ServiceBusEntity entity,
            TestConfig config,
            ITestOutputHelper outputWriter)
        {
            var service = new TestMessagePumpService(entity, config, outputWriter);
            await service.StartAsync();

            return service;
        }

        private async Task StartAsync()
        {
            if (_serviceBusEventConsumerHost is null)
            {
                var topicName = _configuration.GetValue<string>("Arcus:Infra:ServiceBus:TopicName");
                var connectionString = _configuration.GetValue<string>("Arcus:Infra:ServiceBus:ConnectionString");
                var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(topicName, connectionString);

                _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, new XunitTestLogger(_outputWriter));
            }
            else
            {
                throw new InvalidOperationException("Service is already started!");
            }
        }

        /// <summary>
        /// Simulate the message processing of the message pump using the Service Bus.
        /// </summary>
        public async Task SimulateMessageProcessingAsync()
        {
            if (_serviceBusEventConsumerHost is null)
            {
                throw new InvalidOperationException(
                    "Cannot simulate the message pump because the service is not yet started; please start this service before simulating");
            }

            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();

            string connectionString = _configuration.GetServiceBusConnectionString(_entity);
            var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            var messageSender = new MessageSender(serviceBusConnectionStringBuilder);

            try
            {
                Order order = OrderGenerator.Generate();
                Message orderMessage = order.WrapInServiceBusMessage(operationId, transactionId);
                await messageSender.SendAsync(orderMessage);

                string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(operationId);
                Assert.NotEmpty(receivedEvent);

                EventGridEventBatch<OrderCreatedEvent> eventBatch = EventGridParser.Parse<OrderCreatedEvent>(receivedEvent);
                Assert.NotNull(eventBatch);
                OrderCreatedEvent orderCreatedEvent = Assert.Single(eventBatch.Events);
                Assert.NotNull(orderCreatedEvent);

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
            finally
            {
                await messageSender.CloseAsync();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_serviceBusEventConsumerHost != null)
            {
                await _serviceBusEventConsumerHost.StopAsync();
            }
        }
    }
}
