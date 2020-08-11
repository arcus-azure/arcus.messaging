using System;
using System.Threading.Tasks;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Testing.Logging;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    /// <summary>
    /// Represents a service to interact with the hosted-service.
    /// </summary>
    public class TestMessagePumpService : IAsyncDisposable
    {
        private readonly ILogger _logger;
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        private TestMessagePumpService(TestConfig configuration, ILogger outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(logger, nameof(logger));

            _outputWriter = outputWriter;
            _configuration = configuration;
            _outputWriter = outputWriter;
        }

        /// <summary>
        /// Starts a new instance of the <see cref="TestMessagePumpService"/> type to simulate messages.
        /// </summary>
        /// <param name="config">The configuration instance to retrieve the Azure Service Bus test infrastructure authentication information.</param>
        /// <param name="outputWriter">The instance to log diagnostic messages during the interaction with teh Azure Service Bus test infrastructure.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> or the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static async Task<TestMessagePumpService> StartNewAsync(
            TestConfig config,
            ILogger logger)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(logger, nameof(logger));

            var service = new TestMessagePumpService(config, logger);
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

                _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.StartAsync(serviceBusEventConsumerHostOptions, _logger);
            }
            else
            {
                throw new InvalidOperationException("Service is already started!");
            }
        }

        /// <summary>
        /// Simulate the message processing of the message pump using the Azure Service Bus.
        /// </summary>
        /// <param name="connectionString">The connection string used to send a Azure Service Bus message to the respectively running message pump.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString"/> is blank.</exception>
        public async Task SimulateMessageProcessingAsync(string connectionString)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString));

            if (_serviceBusEventConsumerHost is null)
            {
                throw new InvalidOperationException(
                    "Cannot simulate the message pump because the service is not yet started; please start this service before simulating");
            }

            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();

            var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            var messageSender = new MessageSender(serviceBusConnectionStringBuilder);

            try
            {
                Order order = OrderGenerator.Generate();
                Message orderMessage = order.AsServiceBusMessage(operationId, transactionId);
                orderMessage.UserProperties["Topic"] = "Orders";

                await messageSender.SendAsync(orderMessage);

                string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(operationId, 10);
                Assert.NotEmpty(receivedEvent);

                EventBatch<Event> eventBatch = EventParser.Parse(receivedEvent);
                Assert.NotNull(eventBatch);
                Event orderCreatedEvent = Assert.Single(eventBatch.Events);
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
