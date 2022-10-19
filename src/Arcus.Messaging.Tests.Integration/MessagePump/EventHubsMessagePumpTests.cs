using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.EventHubs;
using Arcus.Messaging.Pumps.EventHubs.Configuration;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.Fixture.Logging;
using Arcus.Messaging.Tests.Integration.MessagePump.EventHubs;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageHandlers;
using Arcus.Messaging.Tests.Workers.MessageBodyHandlers;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers;
using Arcus.Testing.Logging;
using Azure.Messaging.EventHubs;
using Bogus;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Integration")]
    [Trait("Category", "Integration")]
    public class EventHubsMessagePumpTests : IAsyncLifetime
    {
        private readonly TestConfig _config;
        private readonly ILogger _logger;

        private static readonly Faker BogusGenerator = new Faker();

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
        public async Task EventHubsMessagePump_PublishMessageForHierarchical_MessageSuccessfullyProcessed()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs, opt => opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical)
               .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            // Act / Assert
            await TestEventHubsMessageHandlingForHierarchicalAsync(options, eventHubs);
        }

        [Fact]
        public async Task EventHubsMessagePump_PublishMessageForW3C_MessageSuccessfullyProcessed()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs, opt => opt.Routing.Correlation.Format = MessageCorrelationFormat.W3C)
                .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            // Act / Assert
            await TestEventHubsMessageHandlingForW3CAsync(options, eventHubs);
        }

        [Fact]
        public async Task EventHubsMessagePumpWithMessageContextFilter_PublishesMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs)
                .WithEventHubsMessageHandler<TestEventHubsMessageHandler<Order>, Order>(messageContextFilter: context => false)
                .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            // Act / Assert
            await TestEventHubsMessageHandlingForW3CAsync(options, eventHubs);
        }

        [Fact]
        public async Task EventHubsMessagePumpWithMessageFilter_PublishesMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs)
                .WithEventHubsMessageHandler<TestEventHubsMessageHandler<Order>, Order>(messageBodyFilter: body => false)
                .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            // Act / Assert
            await TestEventHubsMessageHandlingForW3CAsync(options, eventHubs);
        }

        [Fact]
        public async Task EventHubsMessagePumpWithDifferentMessageType_PublishesMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs)
                .WithEventHubsMessageHandler<TestEventHubsMessageHandler<Shipment>, Shipment>()
                .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            // Act / Assert
            await TestEventHubsMessageHandlingForW3CAsync(options, eventHubs);
        }

        [Fact]
        public async Task EventHubsMessagePumpWithMessageBodySerializer_PublishesMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs)
                .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>(messageBodySerializerImplementationFactory: provider =>
                {
                    var logger = provider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                    return new OrderBatchMessageBodySerializer(logger);
                });

            // Act / Assert
            await TestEventHubsMessageHandlingForW3CAsync(options, eventHubs);
        }

        [Fact]
        public async Task EventHubsMessagePumpWithFallback_PublishesMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs)
                .WithEventHubsMessageHandler<TestEventHubsMessageHandler<Shipment>, Shipment>()
                .WithFallbackMessageHandler<OrderEventHubsFallbackMessageHandler>();

            // Act / Assert
            await TestEventHubsMessageHandlingForW3CAsync(options, eventHubs);
        }

        [Fact]
        public async Task RestartedEventHubsMessagePump_PublishMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs)
                .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            var traceParent = TraceParent.Generate();
            EventData expected = CreateOrderEventDataMessageForW3C(traceParent);
            string eventHubsName = eventHubs.GetEventHubsName(IntegrationTestType.SelfContained);
            var producer = new TestEventHubsMessageProducer(eventHubs.EventHubsConnectionString, eventHubsName);

            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                IEnumerable<AzureEventHubsMessagePump> messagePumps = 
                    worker.Services.GetServices<IHostedService>()
                                   .OfType<AzureEventHubsMessagePump>();

                AzureEventHubsMessagePump messagePump = Assert.Single(messagePumps);
                Assert.NotNull(messagePump);

                await messagePump.RestartAsync(CancellationToken.None);

                // Act
                await producer.ProduceAsync(expected);

                // Assert
                OrderCreatedEventData actual = consumer.ConsumeOrderEventForW3C(traceParent.TransactionId);
                AssertReceivedOrderEventDataForW3C(expected, actual, traceParent);
            }
        }

        [Fact]
        public async Task EventHubsMessagePumpWithAll_PublishesMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            string eventHubsName = eventHubs.GetEventHubsName(IntegrationTestType.SelfContained);
            var properties = EventHubsConnectionStringProperties.Parse(eventHubs.EventHubsConnectionString);
            
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs)
                .WithEventHubsMessageHandler<TestEventHubsMessageHandler<Shipment>, Shipment>()
                .WithEventHubsMessageHandler<TestEventHubsMessageHandler<Order>, Order>(messageBodyFilter: body => false)
                .WithEventHubsMessageHandler<TestEventHubsMessageHandler<Order>, Order>(messageContextFilter: body => false)
                .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>(
                    messageContextFilter: context => context.ConsumerGroup == "$Default" 
                                                     && context.EventHubsName == eventHubsName
                                                     && context.EventHubsNamespace == properties.FullyQualifiedNamespace,
                    messageBodySerializerImplementationFactory: provider =>
                    {
                        var logger = provider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                        return new OrderBatchMessageBodySerializer(logger);
                    },
                    messageBodyFilter: order => Guid.TryParse(order.Id, out Guid _),
                    messageHandlerImplementationFactory: provider =>
                    {
                        return new OrderEventHubsMessageHandler(
                            provider.GetRequiredService<IEventGridPublisher>(),
                            provider.GetRequiredService<IMessageCorrelationInfoAccessor>(),
                            provider.GetRequiredService<ILogger<OrderEventHubsMessageHandler>>());
                    });

            // Act / Assert
            await TestEventHubsMessageHandlingForW3CAsync(options, eventHubs);
        }

        [Fact]
        public async Task EventHubsMessagePump_WithCustomTransactionIdProperty_RetrievesCorrelationCorrectlyDuringMessageProcessing()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var customTransactionIdPropertyName = "MyTransactionId";
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs, opt =>
                {
                    opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                    opt.Routing.Correlation.TransactionIdPropertyName = customTransactionIdPropertyName;
                })
                .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            // Act / Assert
            await TestEventHubsMessageHandlingForHierarchicalAsync(options, eventHubs, customTransactionIdPropertyName);
        }

        [Fact]
        public async Task EventHubsMessagePump_WithCustomOperationParentIdProperty_RetrievesCorrelationCorrectlyDuringMessageProcessing()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var customOperationParentIdPropertyName = "MyOperationParentId";
            var options = new WorkerOptions();
            AddEventHubsMessagePump(options, eventHubs, opt =>
                {
                    opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                    opt.Routing.Correlation.OperationParentIdPropertyName = customOperationParentIdPropertyName;
                })
                .WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>();

            // Act / Assert
            await TestEventHubsMessageHandlingForHierarchicalAsync(options, eventHubs, operationParentIdPropertyName: customOperationParentIdPropertyName);
        }

        [Fact]
        public async Task EventHubsMessagePump_WithW3CCorrelationFormat_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();

            var options = new WorkerOptions();
            options.Configure(host => host.UseSerilog((context, config) =>
            {
                config.MinimumLevel.Debug()
                      .Enrich.FromLogContext()
                      .WriteTo.ApplicationInsights(spySink);
            }));
            AddEventHubsMessagePump(options, eventHubs)
                .WithEventHubsMessageHandler<OrderWithAutoTrackingEventHubsMessageHandler, Order>();
            options.Services.Configure<TelemetryConfiguration>(conf => conf.TelemetryChannel = spyChannel);

            Order order = OrderGenerator.Generate();
            var traceParent = TraceParent.Generate();
            var eventData = new EventData(BinaryData.FromObjectAsJson(order)).WithDiagnosticId(traceParent);

            string eventHubsName = eventHubs.GetEventHubsName(IntegrationTestType.SelfContained);
            var producer = new TestEventHubsMessageProducer(eventHubs.EventHubsConnectionString, eventHubsName);

            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(eventData);

                // Assert
                AssertX.RetryAssertUntilTelemetryShouldBeAvailable(() =>
                {
                    RequestTelemetry requestViaArcusEventHubs = AssertX.GetRequestFrom(spySink.Telemetries, r => r.Name == "Process" && r.Context.Operation.Id == traceParent.TransactionId);
                    DependencyTelemetry dependencyViaArcusKeyVault = AssertX.GetDependencyFrom(spySink.Telemetries, d => d.Type == "Azure key vault" && d.Context.Operation.Id == traceParent.TransactionId);
                    DependencyTelemetry dependencyViaMicrosoftSql = AssertX.GetDependencyFrom(spyChannel.Telemetries, d => d.Type == "SQL" && d.Context.Operation.Id == traceParent.TransactionId);
                    
                    Assert.Equal(requestViaArcusEventHubs.Id, dependencyViaArcusKeyVault.Context.Operation.ParentId);
                    Assert.Equal(requestViaArcusEventHubs.Id, dependencyViaMicrosoftSql.Context.Operation.ParentId);
                }, timeout: TimeSpan.FromMinutes(1), _logger);
            }
        }

         [Fact]
        public async Task EventHubsMessagePump_WithW3CCorrelationFormatForNewParent_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var options = new WorkerOptions();
            options.Configure(host => host.UseSerilog((context, config) =>
            {
                config.MinimumLevel.Debug()
                      .Enrich.FromLogContext()
                      .WriteTo.ApplicationInsights(spySink);
            }));

            EventHubsConfig eventHubs = _config.GetEventHubsConfig();
            AddEventHubsMessagePump(options, eventHubs)
                .WithEventHubsMessageHandler<OrderWithAutoTrackingEventHubsMessageHandler, Order>();

            var spyChannel = new InMemoryTelemetryChannel();
            options.Services.Configure<TelemetryConfiguration>(conf => conf.TelemetryChannel = spyChannel);

            Order order = OrderGenerator.Generate();
            var eventData = new EventData(BinaryData.FromObjectAsJson(order));

            string eventHubsName = eventHubs.GetEventHubsName(IntegrationTestType.SelfContained);
            var producer = new TestEventHubsMessageProducer(eventHubs.EventHubsConnectionString, eventHubsName);

            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(eventData);

                // Assert
                AssertX.RetryAssertUntilTelemetryShouldBeAvailable(() =>
                {
                    IEnumerable<DependencyTelemetry> dependenciesViaArcusKeyVault = spySink.Telemetries.OfType<DependencyTelemetry>().Where(d => d.Type == "Azure key vault");
                    IEnumerable<DependencyTelemetry> dependenciesViaMicrosoftSql = spyChannel.Telemetries.OfType<DependencyTelemetry>().Where(d => d.Type == "SQL");

                    bool correlationSuccess = spySink.Telemetries.Any(t =>
                    {
                        return t is RequestTelemetry r && r.Name == "Process" 
                               && dependenciesViaArcusKeyVault.SingleOrDefault(d => d.Context.Operation.ParentId == r.Id) != null
                               && dependenciesViaMicrosoftSql.SingleOrDefault(d => d.Context.Operation.ParentId == r.Id) != null;
                    });
                    Assert.True(correlationSuccess);
                }, timeout: TimeSpan.FromMinutes(1), _logger);
            }
        }

        private EventHubsMessageHandlerCollection AddEventHubsMessagePump(WorkerOptions options, EventHubsConfig eventHubs, Action<AzureEventHubsMessagePumpOptions> configureOptions = null)
        {
            string eventHubsName = eventHubs.GetEventHubsName(IntegrationTestType.SelfContained);
            string eventHubsConnectionStringSecretName = "Arcus_EventHubs_ConnectionString",
                   storageAccountConnectionStringSecretName = "Arcus_StorageAccount_ConnectionString";

            return options.AddEventGridPublisher(_config)
                          .ConfigureLogging(_logger)
                          .AddSecretStore(stores => stores.AddInMemory(new Dictionary<string, string>
                          {
                              [eventHubsConnectionStringSecretName] = eventHubs.EventHubsConnectionString,
                              [storageAccountConnectionStringSecretName] = eventHubs.StorageConnectionString
                          }))
                          .AddEventHubsMessagePump(eventHubsName, eventHubsConnectionStringSecretName, ContainerName, storageAccountConnectionStringSecretName, configureOptions);
        }

        private async Task TestEventHubsMessageHandlingForHierarchicalAsync(
            WorkerOptions options, 
            EventHubsConfig eventHubs, 
            string transactionIdPropertyName = PropertyNames.TransactionId, 
            string operationParentIdPropertyName = PropertyNames.OperationParentId)
        {
            EventData expected = CreateOrderEventDataMessageForHierarchical(transactionIdPropertyName, operationParentIdPropertyName);
            string eventHubsName = eventHubs.GetEventHubsName(IntegrationTestType.SelfContained);
            var producer = new TestEventHubsMessageProducer(eventHubs.EventHubsConnectionString, eventHubsName);

            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(expected);

                // Assert
                OrderCreatedEventData actual = consumer.ConsumeOrderEventForHierarchical(expected.CorrelationId);
                AssertReceivedOrderEventDataForHierarchical(expected, actual, transactionIdPropertyName, operationParentIdPropertyName);
            }
        }

        private async Task TestEventHubsMessageHandlingForW3CAsync(
            WorkerOptions options,
            EventHubsConfig eventHubs)
        {
            var traceParent = TraceParent.Generate();
            EventData expected = CreateOrderEventDataMessageForW3C(traceParent);
            
            string eventHubsName = eventHubs.GetEventHubsName(IntegrationTestType.SelfContained);
            var producer = new TestEventHubsMessageProducer(eventHubs.EventHubsConnectionString, eventHubsName);

            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(expected);

                // Assert
                OrderCreatedEventData actual = consumer.ConsumeOrderEventForW3C(traceParent.TransactionId);
                AssertReceivedOrderEventDataForW3C(expected, actual, traceParent);
            }
        }

        private static EventData CreateOrderEventDataMessageForHierarchical(
            string transactionIdPropertyName = PropertyNames.TransactionId, 
            string operationParentIdPropertyName = PropertyNames.OperationParentId)
        {
            Order order = OrderGenerator.Generate();
            var eventData = new EventData(JsonConvert.SerializeObject(order));
            eventData.MessageId = $"message-{Guid.NewGuid()}";
            eventData.CorrelationId = $"operation-{Guid.NewGuid()}";
            eventData.Properties[transactionIdPropertyName] = $"transaction-{Guid.NewGuid()}";
            eventData.Properties[operationParentIdPropertyName] = $"parent-{Guid.NewGuid()}";

            return eventData;
        }

        private static EventData CreateOrderEventDataMessageForW3C(TraceParent traceParent)
        {
            Order order = OrderGenerator.Generate();
            return new EventData(JsonConvert.SerializeObject(order)).WithDiagnosticId(traceParent);
        }

        private static void AssertReceivedOrderEventDataForHierarchical(
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

        private static void AssertReceivedOrderEventDataForW3C(
            EventData message,
            OrderCreatedEventData receivedEventData,
            TraceParent traceParent,
            Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            string json = encoding.GetString(message.EventBody);
            var order = JsonConvert.DeserializeObject<Order>(json);

            Assert.NotNull(receivedEventData);
            Assert.NotNull(receivedEventData.CorrelationInfo);
            Assert.Equal(order.Id, receivedEventData.Id);
            Assert.Equal(order.Amount, receivedEventData.Amount);
            Assert.Equal(order.ArticleNumber, receivedEventData.ArticleNumber);
            Assert.Equal(traceParent.TransactionId, receivedEventData.CorrelationInfo.TransactionId);
            Assert.NotNull(receivedEventData.CorrelationInfo.OperationId);
            Assert.Equal(traceParent.OperationParentId, receivedEventData.CorrelationInfo.OperationParentId);
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
