using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Messaging.Tests.Core.Correlation;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.Fixture.Logging;
using Arcus.Messaging.Tests.Integration.MessagePump.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Integration.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageBodyHandlers;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers;
using Arcus.Observability.Telemetry.Core;
using Arcus.Security.Core.Caching.Configuration;
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Security.KeyVault.Secrets;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Microsoft.ApplicationInsights.DataContracts;
using System.Linq;
using System.Runtime.CompilerServices;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Testing;
using Bogus;
using Microsoft.VisualBasic;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
using TestConfig = Arcus.Messaging.Tests.Integration.Fixture.TestConfig;
using XunitTestLogger = Arcus.Testing.Logging.XunitTestLogger;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Integration")]
    [Trait("Category", "Integration")]
    public class ServiceBusMessagePumpTests
    {
        private readonly TestConfig _config;
        private readonly ILogger _logger;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessagePumpTests"/> class.
        /// </summary>
        public ServiceBusMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _config = TestConfig.Create();
            _outputWriter = outputWriter;
            _logger = new XunitTestLogger(outputWriter);
        }

        private string QueueConnectionString => _config.GetServiceBusQueueConnectionString();
        private string TopicConnectionString => _config.GetServiceBusTopicConnectionString();

        public static IEnumerable<object[]> Encodings
        {
            get
            {
                yield return new object[] { Encoding.UTF8 };
                yield return new object[] { Encoding.UTF32 };
                yield return new object[] { Encoding.ASCII };
                yield return new object[] { Encoding.Unicode };
                yield return new object[] { Encoding.BigEndianUnicode };
            }
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task ServiceBusQueueMessagePump_PublishesEncodedServiceBusMessage_MessageSuccessfullyProcessed(Encoding encoding)
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent, encoding);

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            await using (var _ = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                OrderCreatedEventData eventData = consumer.ConsumeOrderEventForW3C(traceParent.TransactionId);
                AssertReceivedOrderEventDataForW3C(message, eventData, traceParent, encoding);
            }
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task ServiceBusTopicMessagePump_PublishesEncodedServiceBusMessage_MessageSuccessfullyProcessed(Encoding encoding)
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePump(TopicConnectionString)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent, encoding);

            var producer = TestServiceBusMessageProducer.CreateForTopic(_config);
            await using (var _ = await Worker.StartNewAsync(options)) 
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                OrderCreatedEventData eventData = consumer.ConsumeOrderEventForW3C(traceParent.TransactionId);
                AssertReceivedOrderEventDataForW3C(message, eventData, traceParent, encoding);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessageForHierarchical_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt =>
                   {
                       opt.AutoComplete = true;
                       opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                   })
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue, MessageCorrelationFormat.Hierarchical);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithNamespaceScopedConnectionString_PublishesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var properties = ServiceBusConnectionStringProperties.Parse(QueueConnectionString);
            string namespaceConnectionString = properties.GetNamespaceConnectionString();

            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(properties.EntityPath, _ => namespaceConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            
            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePump(TopicConnectionString)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            
            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Topic);
        }

        [Theory]
        [InlineData(TopicSubscription.None, false)]
        [InlineData(TopicSubscription.Automatic, true)]
        public async Task ServiceBusTopicMessagePump_WithNoneTopicSubscription_DoesntCreateTopicSubscription(TopicSubscription topicSubscription, bool doesSubscriptionExists)
        {
            // Arrange
            var options = new WorkerOptions();
            var subscriptionName = $"Subscription-{Guid.NewGuid():N}";
            options.AddServiceBusTopicMessagePump(
                       subscriptionName, 
                       _ => TopicConnectionString, 
                       opt => opt.TopicSubscription = topicSubscription)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            {
                var client = new ServiceBusAdministrationClient(TopicConnectionString);
                var properties = ServiceBusConnectionStringProperties.Parse(TopicConnectionString);
                
                Response<bool> subscriptionExistsResponse = await client.SubscriptionExistsAsync(properties.EntityPath, subscriptionName);
                Assert.Equal(doesSubscriptionExists, subscriptionExistsResponse.Value);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePump_WithCustomTransactionIdProperty_RetrievesCorrelationCorrectlyDuringMessageProcessing()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Topic;
            var customTransactionIdPropertyName = "MyTransactionId";
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePump(
                       $"MySubscription-{Guid.NewGuid():N}",
                       _ => TopicConnectionString,
                       opt =>
                       {
                           opt.AutoComplete = true;
                           opt.TopicSubscription = TopicSubscription.Automatic;
                           opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                           opt.Routing.Correlation.TransactionIdPropertyName = customTransactionIdPropertyName;
                       })
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage message = CreateOrderServiceBusMessageForHierarchical(customTransactionIdPropertyName);
            
            var producer = TestServiceBusMessageProducer.CreateFor(_config, entityType);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                var consumer = new DiskMessageEventConsumer();
                OrderCreatedEventData eventData = await consumer.ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForHierarchical(message, eventData, transactionIdPropertyName: customTransactionIdPropertyName);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithSubscriptionNameOver50_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePump(
                       subscriptionName: "Test-Receive-All-Topic-Only-with-an-azure-servicebus-topic-subscription-name-over-50-characters", 
                       _ => TopicConnectionString, 
                       opt =>
                       {
                           opt.AutoComplete = true;
                           opt.TopicSubscription = TopicSubscription.Automatic;
                       })
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            
            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Topic);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithNamespaceScopedConnectionString_PublishesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var properties = ServiceBusConnectionStringProperties.Parse(TopicConnectionString);
            string namespaceConnectionString = properties.GetNamespaceConnectionString();

            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePump(
                       topicName: properties.EntityPath,
                       subscriptionName: Guid.NewGuid().ToString(),
                       getConnectionStringFromConfigurationFunc: _ => namespaceConnectionString,
                       configureMessagePump: opt =>
                       {
                           opt.AutoComplete = true;
                           opt.TopicSubscription = TopicSubscription.Automatic;
                       })
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Topic);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithIgnoringMissingMembersDeserialization_PublishesServiceBusMessage_MessageGetsProcessedByDifferentMessageHandler()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(
                       _ => QueueConnectionString, 
                       opt => opt.Routing.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore)
                   .WithServiceBusMessageHandler<WriteOrderV2ToDiskAzureServiceBusMessageHandler, OrderV2>();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpUsingManagedIdentity_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            ServiceBusConnectionStringProperties properties = ServiceBusConnectionStringProperties.Parse(TopicConnectionString);

            using var auth = TemporaryManagedIdentityConnection.Create(_config, _logger);
            
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePumpUsingManagedIdentity(
                       topicName: properties.EntityPath,
                       subscriptionName: Guid.NewGuid().ToString(),
                       serviceBusNamespace: properties.FullyQualifiedNamespace,
                       clientId: auth.ClientId,
                       configureMessagePump: opt =>
                       {
                           opt.AutoComplete = true;
                           opt.TopicSubscription = TopicSubscription.Automatic;
                       })
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Topic);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithCustomComplete_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePump(TopicConnectionString)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            
            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Topic);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithCustomCompleteOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(
                       _ => QueueConnectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                   .WithServiceBusFallbackMessageHandler<CompleteAzureServiceBusFallbackMessageHandler>();
            
            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePump_WithCustomOperationParentIdProperty_RetrievesCorrelationCorrectlyDuringMessageProcessing()
        {
            // Arrange
            var customOperationParentIdPropertyName = "MyOperationParentId";
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(
                       _ => QueueConnectionString,
                       opt =>
                       {
                           opt.AutoComplete = true;
                           opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                           opt.Routing.Correlation.OperationParentIdPropertyName = customOperationParentIdPropertyName;
                       })
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage message =
                CreateOrderServiceBusMessageForHierarchical(operationParentIdPropertyName: customOperationParentIdPropertyName);

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                OrderCreatedEventData eventData = consumer.ConsumeOrderEventForHierarchical(message.CorrelationId);
                AssertReceivedOrderEventDataForHierarchical(message, eventData, operationParentIdPropertyName: customOperationParentIdPropertyName);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpUsingManagedIdentity_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            ServiceBusConnectionStringProperties properties = ServiceBusConnectionStringProperties.Parse(QueueConnectionString);

            using var auth = TemporaryManagedIdentityConnection.Create(_config, _logger);
            
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePumpUsingManagedIdentity(
                       queueName: properties.EntityPath,
                       serviceBusNamespace: properties.FullyQualifiedNamespace,
                       clientId: auth.ClientId,
                       configureMessagePump: opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithCustomCompleteOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
                   .WithServiceBusFallbackMessageHandler<CompleteAzureServiceBusFallbackMessageHandler>();
            
            // Act / Assert
            await TestServiceBusQueueCompletedMessageAsync(options);
        }

        private async Task TestServiceBusQueueCompletedMessageAsync(WorkerOptions options)
        {
            options.AddXunitTestLogging(_outputWriter);
            await using var worker = await Worker.StartNewAsync(options);

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);

            TraceParent traceParent = TraceParent.Generate();
            var message = CreateOrderServiceBusMessageForW3C(traceParent);
            
            // Act
            await producer.ProduceAsync(message);

            // Assert
            var consumer = TestServiceMessageConsumer.CreateForQueue(_config, _logger);
            await consumer.AssertCompletedMessageAsync(message.MessageId);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithBatchedMessages_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(
                       messageBodySerializerImplementationFactory: serviceProvider =>
                        {
                            var logger = serviceProvider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                            return new OrderBatchMessageBodySerializer(logger);
                        });
            
            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextTypeFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextAndBodyFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.ContainsKey("NotExisting"), _ => false)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>(
                       context => context.Properties["Topic"].ToString() == "Orders", 
                       body => body.Id != null);

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);
            message.ApplicationProperties["Topic"] = "Orders";

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                var consumer = new DiskMessageEventConsumer();
                OrderCreatedEventData eventData = await consumer.ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData, traceParent);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithContextFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .AddServiceBusTopicMessagePump(TopicConnectionString)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Customers")
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Orders")
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext _) => false);

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);
            message.ApplicationProperties["Topic"] = "Orders";

            var producer = TestServiceBusMessageProducer.CreateForTopic(_config);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                var consumer = new DiskMessageEventConsumer();
                OrderCreatedEventData eventData = await consumer.ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData, traceParent);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithBodyFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePump(TopicConnectionString)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>((Customer body) => body is null)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>((Order body) => body.Id != null)
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((Order _) => false);

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Topic);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextAndBodyFilteringWithSerializer_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>(messageContextFilter: _ => false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(messageBodyFilter: _ => true)
                   .WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(
                       messageContextFilter: context => context != null,
                       messageBodySerializerImplementationFactory: serviceProvider =>
                       {
                           var logger = serviceProvider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                           return new OrderBatchMessageBodySerializer(logger);
                       },
                       messageBodyFilter: message => message.Orders.Length == 1);

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
        }

        [Fact(Skip = ".NET application cannot start multiple blocking background tasks, see https://github.com/dotnet/runtime/issues/36063")]
        public async Task ServiceBusMessagePumpWithQueueAndTopic_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config);
            options.AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            options.AddServiceBusTopicMessagePump(
                subscriptionName: Guid.NewGuid().ToString(),
                _ => _config.GetServiceBusConnectionString(ServiceBusEntityType.Topic),
                opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Topic);
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithFallbackMessageHandler<WriteOrderToDiskFallbackMessageHandler>();
            
            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithServiceBusFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithServiceBusFallbackMessageHandler<WriteOrderToDiskFallbackMessageHandler>();

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetter_PublishServiceBusMessage_MessageSuccessfullyDeadLettered()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<DeadLetterAzureServiceMessageHandler, Order>();

            await TestServiceBusQueueDeadLetteredMessageAsync(options);
        }

        private async Task TestServiceBusQueueDeadLetteredMessageAsync(WorkerOptions options)
        {
            options.AddXunitTestLogging(_outputWriter);

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);

            // Act
            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            var consumer = TestServiceMessageConsumer.CreateForQueue(_config, _logger);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                await consumer.AssertDeadLetterMessageAsync(message.MessageId);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetterOnFallback_PublishServiceBusMessage_MessageSuccessfullyDeadLettered()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(
                       _ => connectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>((AzureServiceBusMessageContext _) => true)
                   .WithServiceBusFallbackMessageHandler<OrdersAzureServiceBusDeadLetterFallbackMessageHandler>();

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            var consumer = TestServiceMessageConsumer.CreateForQueue(_config, _logger);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                await consumer.AssertDeadLetterMessageAsync(message.MessageId);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetterAfterContextPredicate_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties["Topic"].ToString() == "Customers")
                   .WithServiceBusMessageHandler<DeadLetterAzureServiceMessageHandler, Order>(context => context.Properties["Topic"].ToString() == "Orders")
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext _) => false);

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            var consumer = TestServiceMessageConsumer.CreateForQueue(_config, _logger);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                await consumer.AssertDeadLetterMessageAsync(message.MessageId);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusAbandon_PublishServiceBusMessage_MessageSuccessfullyAbandoned()
        {
            await TestServiceBusQueueAbandonMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePump(_ => _config.GetServiceBusQueueConnectionString())
                       .WithServiceBusMessageHandler<AbandonAzureServiceBusMessageHandler, Order>();
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusAbandonOnFallback_PublishServiceBusMessage_MessageSuccessfullyAbandoned()
        {
            await TestServiceBusQueueAbandonMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePump(_ => _config.GetServiceBusQueueConnectionString())
                       .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                       .WithServiceBusFallbackMessageHandler<AbandonAzureServiceBusFallbackMessageHandler>();
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusAbandonAfterContextPredicate_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusQueueAbandonMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePump(_ => _config.GetServiceBusQueueConnectionString())
                       .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                       .WithServiceBusMessageHandler<AbandonAzureServiceBusMessageHandler, Order>((AzureServiceBusMessageContext _) => true);
            });
        }

        private async Task TestServiceBusQueueAbandonMessageAsync(Action<WorkerOptions> configureOptions)
        {
            var options = new WorkerOptions();
            configureOptions(options);

            options.AddXunitTestLogging(_outputWriter);
            await using var worker = await Worker.StartNewAsync(options);

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);

            TraceParent traceParent = TraceParent.Generate();
            var message = CreateOrderServiceBusMessageForW3C(traceParent);
            
            // Act
            await producer.ProduceAsync(message);

            // Assert
            var consumer = TestServiceMessageConsumer.CreateForQueue(_config, _logger);
            await consumer.AssertAbandonMessageAsync(message.MessageId);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePump_PauseViaCircuitBreaker_RestartsAgainWithOneMessage()
        {
            // Arrange
            var options = new WorkerOptions();
            ServiceBusMessage[] messages = GenerateShipmentMessages(3);
            TimeSpan recoveryTime = TimeSpan.FromSeconds(10);
            TimeSpan messageInterval = TimeSpan.FromSeconds(2);

            options.AddXunitTestLogging(_outputWriter)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: "circuit-breaker-" + Guid.NewGuid(),
                       _ => _config.GetServiceBusTopicConnectionString(),
                       opt => opt.TopicSubscription = TopicSubscription.Automatic)
                   .WithServiceBusMessageHandler<CircuitBreakerAzureServiceBusMessageHandler, Shipment>(
                        implementationFactory: provider => new CircuitBreakerAzureServiceBusMessageHandler(
                            targetMessageIds: messages.Select(m => m.MessageId).ToArray(),
                            configureOptions: opt =>
                            {
                                opt.MessageRecoveryPeriod = recoveryTime;
                                opt.MessageIntervalDuringRecovery = messageInterval;
                            },
                            provider.GetRequiredService<IMessagePumpCircuitBreaker>()));

            var producer = TestServiceBusMessageProducer.CreateFor(_config, ServiceBusEntityType.Topic);
            await using var worker = await Worker.StartNewAsync(options);

            // Act
            await producer.ProduceAsync(messages);

            // Assert
            var handler = GetMessageHandler<CircuitBreakerAzureServiceBusMessageHandler>(worker);
            AssertX.RetryAssertUntil(() =>
            {
                DateTimeOffset[] arrivals = handler.GetMessageArrivals();
                Assert.Equal(messages.Length, arrivals.Length);

                _outputWriter.WriteLine("Arrivals: {0}", string.Join(", ", arrivals));
                TimeSpan faultMargin = TimeSpan.FromSeconds(1);
                Assert.Collection(arrivals.SkipLast(1).Zip(arrivals.Skip(1)),
                    dates => AssertDateDiff(dates.First, dates.Second, recoveryTime, recoveryTime.Add(faultMargin)),
                    dates => AssertDateDiff(dates.First, dates.Second, messageInterval, messageInterval.Add(faultMargin)));

            }, timeout: TimeSpan.FromMinutes(2), _logger);
        }

        private static TMessageHandler GetMessageHandler<TMessageHandler>(Worker worker)
        {
            return Assert.IsType<TMessageHandler>(
                worker.Services.GetRequiredService<MessageHandler>()
                               .GetMessageHandlerInstance());
        }

        private static void AssertDateDiff(DateTimeOffset left, DateTimeOffset right, TimeSpan expectedMin, TimeSpan expectedMax)
        {
            left = new DateTimeOffset(left.Year, left.Month, left.Day, left.Hour, left.Minute, left.Second, 0, left.Offset);
            right = new DateTimeOffset(right.Year, right.Month, right.Day, right.Hour, right.Minute, right.Second, 0, right.Offset);

            TimeSpan actual = right - left;
            Assert.InRange(actual, expectedMin, expectedMax);
        }

        private static ServiceBusMessage[] GenerateShipmentMessages(int count)
        {
            var generator = new Faker<Shipment>()
                .RuleFor(s => s.Id, f => f.Random.Guid().ToString())
                .RuleFor(s => s.Code, f => f.Random.Int(1, 100))
                .RuleFor(s => s.Date, f => f.Date.RecentOffset())
                .RuleFor(s => s.Description, f => f.Lorem.Sentence());

            return Enumerable.Repeat(generator, count).Select(g =>
            {
                Shipment shipment = g.Generate();
                string json = JsonConvert.SerializeObject(shipment);
                return new ServiceBusMessage(json)
                {
                    MessageId = shipment.Id
                };
            }).ToArray();
        }

        [Fact]
        public async Task ServiceBusMessagePump_PauseViaLifetime_RestartsAgain()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            string jobId = Guid.NewGuid().ToString();
            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: Guid.NewGuid().ToString(), 
                       _ => connectionString, 
                       opt =>
                       {
                           opt.JobId = jobId;
                           opt.TopicSubscription = TopicSubscription.Automatic;
                       })
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>((AzureServiceBusMessageContext _) => true);

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);

            var producer = TestServiceBusMessageProducer.CreateFor(_config, ServiceBusEntityType.Topic);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                var lifetime = worker.Services.GetRequiredService<IMessagePumpLifetime>();
                await lifetime.PauseProcessingMessagesAsync(jobId, TimeSpan.FromSeconds(5), CancellationToken.None);

                // Act
                await producer.ProduceAsync(message);

                // Assert
                var consumer = new DiskMessageEventConsumer();
                OrderCreatedEventData eventData = await consumer.ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData, traceParent);
            }
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithW3CCorrelationFormat_AutomaticallyTracksMicrosoftDependencies()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();

            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();

            var options = new WorkerOptions();
            options.ConfigureSerilog(config =>
            {
                config.WriteTo.ApplicationInsights(spySink);
            });
            string operationName = Guid.NewGuid().ToString();
            options.AddServiceBusQueueMessagePump(_ => connectionString, opt => 
            {
                opt.AutoComplete = true;
                ((AzureServiceBusMessagePumpOptions) opt).Routing.Telemetry.OperationName = operationName;
            }).WithServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler, Order>();
            
            options.Services.Configure<TelemetryConfiguration>(conf => conf.TelemetryChannel = spyChannel);

            Order order = OrderGenerator.Generate();
            var traceParent = TraceParent.Generate();
            var message = new ServiceBusMessage(JsonConvert.SerializeObject(order)).WithDiagnosticId(traceParent);

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                AssertX.RetryAssertUntil(() =>
                {
                    RequestTelemetry requestViaArcusServiceBus = AssertX.GetRequestFrom(spySink.Telemetries, r => r.Name == operationName && r.Context.Operation.Id == traceParent.TransactionId && r.Properties[ContextProperties.RequestTracking.ServiceBus.EntityType] == ServiceBusEntityType.Queue.ToString());
                    DependencyTelemetry dependencyViaArcusKeyVault = AssertX.GetDependencyFrom(spySink.Telemetries, d => d.Type == "Azure key vault" && d.Context.Operation.Id == traceParent.TransactionId);
                    DependencyTelemetry dependencyViaMicrosoftSql = AssertX.GetDependencyFrom(spyChannel.Telemetries, d => d.Type == "SQL" && d.Context.Operation.Id == traceParent.TransactionId);
                    
                    Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaArcusKeyVault.Context.Operation.ParentId);
                    Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaMicrosoftSql.Context.Operation.ParentId);
                }, timeout: TimeSpan.FromMinutes(1), _logger);
            }
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithW3CCorrelationFormatForNewParent_AutomaticallyTracksMicrosoftDependencies()
        {
             // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();

            var spySink = new InMemoryApplicationInsightsTelemetryConverter();
            var spyChannel = new InMemoryTelemetryChannel();

            var options = new WorkerOptions();
            options.ConfigureSerilog(config =>
            {
                config.WriteTo.ApplicationInsights(spySink);
            });
            string operationName = Guid.NewGuid().ToString();
            options.AddServiceBusQueueMessagePump(_ => connectionString, opt =>
            {
                ((AzureServiceBusMessagePumpOptions) opt).Routing.Telemetry.OperationName = operationName;
                opt.AutoComplete = true;                                      
            }).WithServiceBusMessageHandler<OrderWithAutoTrackingAzureServiceBusMessageHandler, Order>();
            
            options.Services.Configure<TelemetryConfiguration>(conf => conf.TelemetryChannel = spyChannel);

            Order order = OrderGenerator.Generate();
            var message = new ServiceBusMessage(JsonConvert.SerializeObject(order));

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                AssertX.RetryAssertUntil(() =>
                {
                    RequestTelemetry requestViaArcusServiceBus = AssertX.GetRequestFrom(spySink.Telemetries, r => r.Name == operationName && r.Properties[ContextProperties.RequestTracking.ServiceBus.EntityType] == ServiceBusEntityType.Queue.ToString());
                    DependencyTelemetry dependencyViaArcusKeyVault = AssertX.GetDependencyFrom(spySink.Telemetries, d => d.Type == "Azure key vault");
                    DependencyTelemetry dependencyViaMicrosoftSql = AssertX.GetDependencyFrom(spyChannel.Telemetries, d => d.Type == "SQL");
                    
                    Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaArcusKeyVault.Context.Operation.ParentId);
                    Assert.Equal(requestViaArcusServiceBus.Id, dependencyViaMicrosoftSql.Context.Operation.ParentId);
                }, timeout: TimeSpan.FromMinutes(1), _logger);
            }
        }

        [Fact]
        public async Task ServiceBusMessagePump_FailureDuringMessageHandling_TracksCorrelationInApplicationInsights()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();

            var spySink = new InMemoryLogSink();
            var options = new WorkerOptions();
            options.ConfigureSerilog(config =>
            {
                config.WriteTo.Sink(spySink);
            });
            options.AddServiceBusQueueMessagePump(_ => connectionString, opt =>
                   {
                       opt.AutoComplete = true;
                       opt.Routing.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                   })
                   .WithServiceBusMessageHandler<OrdersSabotageAzureServiceBusMessageHandler, Order>();
            
            string operationId = $"operation-{Guid.NewGuid()}", transactionId = $"transaction-{Guid.NewGuid()}";
            Order order = OrderGenerator.Generate();
            ServiceBusMessage orderMessage =
                ServiceBusMessageBuilder.CreateForBody(order)
                                        .WithOperationId(operationId)
                                        .WithTransactionId(transactionId)
                                        .Build();

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(orderMessage);
                
                // Assert
                AssertX.RetryAssertUntil(() =>
                {
                    Assert.Contains(spySink.CurrentLogEmits,
                        log => log.Exception?.Message.Contains("Sabotage") is true 
                               && log.ContainsProperty(ContextProperties.Correlation.OperationId, operationId) 
                               && log.ContainsProperty(ContextProperties.Correlation.TransactionId, transactionId));
                },
                timeout: TimeSpan.FromMinutes(1), _logger);
            }
        }

        [Fact]
        public async Task ServiceBusMessagePump_RotateServiceBusConnectionKeys_MessagePumpRestartsThenMessageSuccessfullyProcessed()
        {
            // Arrange
            string tenantId = _config.GetTenantId();
            KeyRotationConfig keyRotationConfig = _config.GetKeyRotationConfig();
            _logger.LogInformation("Using Service Principal [ClientID: '{ClientId}']", keyRotationConfig.ServicePrincipal.ClientId);

            var client = new ServiceBusConfiguration(keyRotationConfig, _logger);
            string freshConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

            SecretClient secretClient = CreateSecretClient(tenantId, keyRotationConfig);
            await SetConnectionStringInKeyVaultAsync(secretClient, keyRotationConfig, freshConnectionString);

            var options = new WorkerOptions();
            options.AddSecretStore(stores => stores.AddAzureKeyVaultWithServicePrincipal(
                       rawVaultUri: keyRotationConfig.KeyVault.VaultUri,
                       tenantId: tenantId,
                       clientId: keyRotationConfig.ServicePrincipal.ClientId,
                       clientKey: keyRotationConfig.ServicePrincipal.ClientSecret,
                       cacheConfiguration: CacheConfiguration.Default))
                   .AddServiceBusQueueMessagePump(keyRotationConfig.KeyVault.SecretName, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                string newSecondaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.SecondaryKey);
                await SetConnectionStringInKeyVaultAsync(secretClient, keyRotationConfig, newSecondaryConnectionString);

                // Act
                string newPrimaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

                // Assert
                var producer = new TestServiceBusMessageProducer(newPrimaryConnectionString);
                await producer.ProduceAsync(message);

                var consumer = new DiskMessageEventConsumer();
                OrderCreatedEventData eventData = await consumer.ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData, traceParent);
            }
        }

        private async Task TestServiceBusMessageHandlingAsync(
            WorkerOptions options, 
            ServiceBusEntityType entityType, 
            MessageCorrelationFormat format = MessageCorrelationFormat.W3C,
            [CallerMemberName] string memberName = null)
        {
            options.AddXunitTestLogging(_outputWriter);

            await using var worker = await Worker.StartNewAsync(options, memberName);
            var consumer = new DiskMessageEventConsumer();

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = format switch
            {
                MessageCorrelationFormat.W3C => CreateOrderServiceBusMessageForW3C(traceParent),
                MessageCorrelationFormat.Hierarchical => CreateOrderServiceBusMessageForHierarchical(),
            };
            var producer = TestServiceBusMessageProducer.CreateFor(_config, entityType);

            // Act
            await producer.ProduceAsync(message);

            // Assert
            OrderCreatedEventData eventData = await consumer.ConsumeOrderCreatedAsync(message.MessageId);
            switch (format)
            {
                case MessageCorrelationFormat.W3C:
                    AssertReceivedOrderEventDataForW3C(message, eventData, traceParent);
                    break;
                
                case MessageCorrelationFormat.Hierarchical:
                    AssertReceivedOrderEventDataForHierarchical(message, eventData);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private static ServiceBusMessage CreateOrderServiceBusMessageForHierarchical(
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentIdPropertyName = PropertyNames.OperationParentId,
            Encoding encoding = null)
        {
            var operationId = $"operation-{Guid.NewGuid()}";
            var transactionId = $"transaction-{Guid.NewGuid()}";
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";

            Order order = OrderGenerator.Generate();
            ServiceBusMessage message =
                ServiceBusMessageBuilder.CreateForBody(order, encoding ?? Encoding.UTF8)
                                        .WithOperationId(operationId)
                                        .WithTransactionId(transactionId, transactionIdPropertyName)
                                        .WithOperationParentId(operationParentId, operationParentIdPropertyName)
                                        .Build();

            message.MessageId = order.Id;
            return message;
        }

        private static ServiceBusMessage CreateOrderServiceBusMessageForW3C(
            TraceParent traceParent,
            Encoding encoding = null)
        {
            Order order = OrderGenerator.Generate();
            string json = JsonConvert.SerializeObject(order);
            encoding ??= Encoding.UTF8;
            byte[] raw = encoding.GetBytes(json);

            var message = new ServiceBusMessage(raw)
            {
                MessageId = order.Id,
                ApplicationProperties =
                {
                    { PropertyNames.ContentType, "application/json" },
                    { PropertyNames.Encoding, encoding.WebName }
                }
            };

            return message.WithDiagnosticId(traceParent);
        }

        private static void AssertReceivedOrderEventDataForHierarchical(
            ServiceBusMessage message,
            OrderCreatedEventData receivedEventData,
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentIdPropertyName = PropertyNames.OperationParentId,
            Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            string json = encoding.GetString(message.Body);

            var order = JsonConvert.DeserializeObject<Order>(json);
            string operationId = message.CorrelationId;
            var transactionId = message.ApplicationProperties[transactionIdPropertyName].ToString();
            var operationParentId = message.ApplicationProperties[operationParentIdPropertyName].ToString();

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
            ServiceBusMessage message,
            OrderCreatedEventData receivedEventData,
            TraceParent traceParent,
            Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            string json = encoding.GetString(message.Body);

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

        private static SecretClient CreateSecretClient(string tenantId, KeyRotationConfig keyRotationConfig)
        {
            var clientCredential = new ClientSecretCredential(tenantId,
                keyRotationConfig.ServicePrincipal.ClientId,
                keyRotationConfig.ServicePrincipal.ClientSecret);
            
            var secretClient = new SecretClient(new Uri(keyRotationConfig.KeyVault.VaultUri), clientCredential);
            return secretClient;
        }

        private static async Task SetConnectionStringInKeyVaultAsync(SecretClient keyVaultClient, KeyRotationConfig keyRotationConfig, string rotatedConnectionString)
        {
            await keyVaultClient.SetSecretAsync(keyRotationConfig.KeyVault.SecretName, rotatedConnectionString);
        }
    }
}