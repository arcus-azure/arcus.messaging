using System;
using System.Threading.Tasks;
using Arcus.EventGrid;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    /// <summary>
    /// Represents a service to interact with the hosted-service.
    /// </summary>
    public class TestMessagePumpService : IAsyncDisposable
    {
        private readonly ILogger _logger;
        private readonly TestConfig _configuration;

        private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

        private TestMessagePumpService(TestConfig configuration, ILogger logger)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(logger, nameof(logger));

            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Starts a new instance of the <see cref="TestMessagePumpService"/> type to simulate messages.
        /// </summary>
        /// <param name="config">The configuration instance to retrieve the Azure Service Bus test infrastructure authentication information.</param>
        /// <param name="logger">The instance to log diagnostic messages during the interaction with teh Azure Service Bus test infrastructure.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> or the <paramref name="logger"/> is <c>null</c>.</exception>
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
        /// <param name="subscriptionName">The topic subscription name when the tested message pump receives messages from an Azure Service Bus Topic.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString"/> is blank.</exception>
        public async Task SimulateMessageProcessingAsync(string connectionString, string subscriptionName = null)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString));

            if (_serviceBusEventConsumerHost is null)
            {
                throw new InvalidOperationException(
                    "Cannot simulate the message pump because the service is not yet started; please start this service before simulating");
            }

            var operationId = Guid.NewGuid().ToString();
            var transactionId = Guid.NewGuid().ToString();

            Order order = OrderGenerator.Generate();
            ServiceBusMessage orderMessage = order.AsServiceBusMessage(operationId, transactionId);
            orderMessage.ApplicationProperties["Topic"] = "Orders";
            await SendMessageToServiceBusAsync(connectionString, orderMessage);

            string receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(operationId, retryCount: 10);
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

            await ClearMessagesAsync(connectionString, subscriptionName);
        }

        /// <summary>
        /// Sends an Azure Service Bus message to the message pump.
        /// </summary>
        /// <param name="connectionString">The connection string to connect to the service bus.</param>
        /// <param name="message">The message to send.</param>
        public async Task SendMessageToServiceBusAsync(string connectionString, ServiceBusMessage message)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString));

            ServiceBusConnectionStringProperties serviceBusConnectionString = ServiceBusConnectionStringProperties.Parse(connectionString);

            await using (var client = new ServiceBusClient(connectionString))
            await using (ServiceBusSender messageSender = client.CreateSender(serviceBusConnectionString.EntityPath))
            {
                await messageSender.SendMessageAsync(message);
            }
        }

        private static async Task ClearMessagesAsync(string connectionString, string subscriptionName = null)
        {
            await using (var client = new ServiceBusClient(connectionString))
            await using (ServiceBusReceiver receiver = CreateServiceBusReceiver(client, connectionString, subscriptionName))
            {
                ServiceBusReceivedMessage message = await receiver.PeekMessageAsync();
                while (message != null)
                {
                    await receiver.CompleteMessageAsync(message);
                    message = await receiver.PeekMessageAsync();
                }
            }
        }

        private static ServiceBusReceiver CreateServiceBusReceiver(
            ServiceBusClient client,
            string connectionString,
            string subscriptionName)
        {
            var properties = ServiceBusConnectionStringProperties.Parse(connectionString);
            if (subscriptionName is null)
            {
                return client.CreateReceiver(properties.EntityPath);
            }

            return client.CreateReceiver(properties.EntityPath, subscriptionName);
        }

        /// <summary>
        /// Tries receiving a single dead lettered message on the Azure Service Bus dead letter queue.
        /// </summary>
        /// <param name="connectionString">The connection string to connect to the Azure Service Bus.</param>
        public async Task AssertDeadLetterMessageAsync(string connectionString)
        {
            var properties = ServiceBusConnectionStringProperties.Parse(connectionString);
            var options = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter };
            
            await using (var client = new ServiceBusClient(connectionString))
            await using (var receiver = client.CreateReceiver(properties.EntityPath, options))
            {
                RetryPolicy<ServiceBusReceivedMessage> retryPolicy =
                    Policy.HandleResult<ServiceBusReceivedMessage>(result => result is null)
                          .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(1));

                await Policy.TimeoutAsync(TimeSpan.FromMinutes(2))
                            .WrapAsync(retryPolicy)
                            .ExecuteAsync(async () =>
                            {
                                ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
                                if (message != null)
                                {
                                    _logger.LogInformation("Received dead lettered message in test suite");
                                    await receiver.CompleteMessageAsync(message);
                                }
                                else
                                {
                                    _logger.LogInformation("No dead lettered message received in test suite, retrying...");
                                }

                                return message;
                            });
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
