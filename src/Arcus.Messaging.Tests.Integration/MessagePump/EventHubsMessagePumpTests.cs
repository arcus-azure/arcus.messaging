using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.EventHubs;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Testing.Logging;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Integration")]
    [Trait("Category", "Integration")]
    public class EventHubsMessagePumpTests : IAsyncLifetime
    {
        private readonly TestConfig _config;
        private readonly ILogger _logger;

        private TemporaryBlobStorageContainer _blobStorageContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubsMessagePumpTests" /> class.
        /// </summary>
        public EventHubsMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _config = TestConfig.Create();
            _logger = new XunitTestLogger(outputWriter);
        }

        private string ContainerName => _blobStorageContainer.ContainerName;

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            _blobStorageContainer = await TemporaryBlobStorageContainer.CreateAsync(eventHubs.StorageConnectionString, _logger);
        }

        [Fact]
        public async Task EventHubsMessagePump_PublishMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            string eventHubsName = eventHubs.GetEventHubsName(IntegrationTestType.SelfContained);
            string eventHubsConnectionStringSecretName = "Arcus_EventHubs_ConnectionString",
                   storageAccountConnectionStringSecretName = "Arcus_StorageAccount_ConnectionString";

            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddSecretStore(stores => stores.AddInMemory(new Dictionary<string, string>
                   {
                       [eventHubsConnectionStringSecretName] = eventHubs.EventHubsConnectionString,
                       [storageAccountConnectionStringSecretName] = eventHubs.StorageConnectionString
                   }))
                   .AddEventHubsMessagePump(eventHubsName, eventHubsConnectionStringSecretName, ContainerName, storageAccountConnectionStringSecretName)
                   .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            EventData expected = CreateOrderEventDataMessage();
            var producer = new TestEventHubsMessageProducer(eventHubs.EventHubsConnectionString, eventHubsName);

            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(expected);

                // Assert
                OrderCreatedEventData actual = consumer.ConsumeOrderEvent(expected.CorrelationId);
                AssertReceivedOrderEventData(expected, actual);
            }
        }

        private static EventData CreateOrderEventDataMessage()
        {
            Order order = OrderGenerator.Generate();
            var eventData = new EventData(JsonConvert.SerializeObject(order));
            eventData.MessageId = $"message-{Guid.NewGuid()}";
            eventData.CorrelationId = $"operation-{Guid.NewGuid()}";
            eventData.Properties[PropertyNames.TransactionId] = $"transaction-{Guid.NewGuid()}";
            eventData.Properties[PropertyNames.OperationParentId] = $"parent-{Guid.NewGuid()}";

            return eventData;
        }

        private static void AssertReceivedOrderEventData(
            EventData message,
            OrderCreatedEventData receivedEventData,
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentIdPropertyName = PropertyNames.OperationParentId,
            Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            string json = encoding.GetString(message.EventBody);

            var order = JsonConvert.DeserializeObject<Order>(json);
            string operationId = message.CorrelationId;
            var transactionId = message.Properties[transactionIdPropertyName].ToString();
            var operationParentId = message.Properties[operationParentIdPropertyName].ToString();

            Assert.NotNull(receivedEventData);
            Assert.NotNull(receivedEventData.CorrelationInfo);
            Assert.Equal(order.Id, receivedEventData.Id);
            Assert.Equal(order.Amount, receivedEventData.Amount);
            Assert.Equal(order.ArticleNumber, receivedEventData.ArticleNumber);
            Assert.Equal(transactionId, receivedEventData.CorrelationInfo.TransactionId);
            Assert.Equal(operationId, receivedEventData.CorrelationInfo.OperationId);
            Assert.Equal(operationParentId, receivedEventData.CorrelationInfo.OperationParentId);
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (_blobStorageContainer != null)
            {
                await _blobStorageContainer.DisposeAsync();
            }
        }
    }
}
