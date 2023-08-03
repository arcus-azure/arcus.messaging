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
using Arcus.Testing.Logging;
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
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = true)
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
            string connectionString = _config.GetServiceBusTopicConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: Guid.NewGuid().ToString(),
                       _ => connectionString,
                       opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            var traceParnet = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParnet, encoding);

            var producer = TestServiceBusMessageProducer.CreateForTopic(_config);
            await using (var _ = await Worker.StartNewAsync(options)) 
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                OrderCreatedEventData eventData = consumer.ConsumeOrderEventForW3C(traceParnet.TransactionId);
                AssertReceivedOrderEventDataForW3C(message, eventData, traceParnet, encoding);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessageForHierarchical_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(_ => connectionString, opt =>
                   {
                       opt.AutoComplete = true;
                       opt.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                   })
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusQueueMessageHandlingForHierarchicalAsync(options);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithNamespaceScopedConnectionString_PublishesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string entityConnectionString = _config.GetServiceBusQueueConnectionString();
            var properties = ServiceBusConnectionStringProperties.Parse(entityConnectionString);
            string namespaceConnectionString = properties.GetNamespaceConnectionString();

            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(properties.EntityPath, _ => namespaceConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            // Act / Assert
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                        subscriptionName: Guid.NewGuid().ToString(),
                        _ => connectionString, 
                        opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            // Act / Assert
            await TestServiceBusTopicMessageHandlingForW3CAsync(options);
        }

        [Theory]
        [InlineData(TopicSubscription.None, false)]
        [InlineData(TopicSubscription.CreateOnStart, true)]
        [InlineData(TopicSubscription.DeleteOnStop, false)]
        [InlineData(TopicSubscription.CreateOnStart | TopicSubscription.DeleteOnStop, true)]
        public async Task ServiceBusTopicMessagePump_WithNoneTopicSubscription_DoesntCreateTopicSubscription(TopicSubscription topicSubscription, bool expected)
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            var options = new WorkerOptions();
            var subscriptionName = $"Subscription-{Guid.NewGuid():N}";
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName, 
                       _ => connectionString, 
                       opt => opt.TopicSubscription = topicSubscription)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            {
                var client = new ServiceBusAdministrationClient(connectionString);
                var properties = ServiceBusConnectionStringProperties.Parse(connectionString);
                
                Response<bool> subscriptionExistsResponse = await client.SubscriptionExistsAsync(properties.EntityPath, subscriptionName);
                if (subscriptionExistsResponse.Value)
                {
                    await client.DeleteSubscriptionAsync(properties.EntityPath, subscriptionName);
                }
                
                Assert.Equal(expected, subscriptionExistsResponse.Value);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePump_WithCustomTransactionIdProperty_RetrievesCorrelationCorrectlyDuringMessageProcessing()
        {
            // Arrange
            var entityType = ServiceBusEntityType.Topic;
            var customTransactionIdPropertyName = "MyTransactionId";
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                       $"MySubscription-{Guid.NewGuid():N}",
                       _ => _config.GetServiceBusConnectionString(entityType),
                       opt =>
                       {
                           opt.AutoComplete = true;
                           opt.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                           opt.Correlation.TransactionIdPropertyName = customTransactionIdPropertyName;
                       })
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage message = CreateOrderServiceBusMessageForHierarchical(customTransactionIdPropertyName);
            
            var producer = TestServiceBusMessageProducer.CreateFor(_config, entityType);
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                OrderCreatedEventData eventData = consumer.ConsumeOrderEventForHierarchical(message.CorrelationId);
                AssertReceivedOrderEventDataForHierarchical(message, eventData, transactionIdPropertyName: customTransactionIdPropertyName);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithSubscriptionNameOver50_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                       "Test-Receive-All-Topic-Only-with-an-azure-servicebus-topic-subscription-name-over-50-characters", 
                       _ => connectionString, 
                       opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            // Act / Assert
            await TestServiceBusTopicMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithNamespaceScopedConnectionString_PublishesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string topicConnectionString = _config.GetServiceBusTopicConnectionString();
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            string namespaceConnectionString = properties.GetNamespaceConnectionString();

            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                       topicName: properties.EntityPath,
                       subscriptionName: Guid.NewGuid().ToString(),
                       getConnectionStringFromConfigurationFunc: _ => namespaceConnectionString,
                       configureMessagePump: opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusTopicMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithIgnoringMissingMembersDeserialization_PublishesServiceBusMessage_MessageGetsProcessedByDifferentMessageHandler()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();

            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(
                       _ => connectionString, 
                       opt => opt.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore)
                   .WithServiceBusMessageHandler<OrderV2AzureServiceBusMessageHandler, OrderV2>();

            // Act / Assert
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpUsingManagedIdentity_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            ServiceBusConnectionStringProperties properties = ServiceBusConnectionStringProperties.Parse(connectionString);

            ServicePrincipal servicePrincipal = _config.GetServiceBusServicePrincipal();
            string tenantId = _config.GetTenantId();

            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, tenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var options = new WorkerOptions();
                options.AddEventGridPublisher(_config)
                       .AddServiceBusTopicMessagePumpUsingManagedIdentity(
                           topicName: properties.EntityPath,
                           subscriptionName: Guid.NewGuid().ToString(),
                           serviceBusNamespace: properties.FullyQualifiedNamespace,
                           clientId: servicePrincipal.ClientId,
                           configureMessagePump: opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

                // Act / Assert
                await TestServiceBusTopicMessageHandlingForW3CAsync(options);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithCustomComplete_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: Guid.NewGuid().ToString(), 
                       _ => connectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusCompleteMessageHandler, Order>();
            
            // Act / Assert
            await TestServiceBusTopicMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithCustomCompleteOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(
                       _ => connectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                   .WithServiceBusFallbackMessageHandler<OrdersFallbackCompleteMessageHandler>();
            
            // Act / Assert
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePump_WithCustomOperationParentIdProperty_RetrievesCorrelationCorrectlyDuringMessageProcessing()
        {
            // Arrange
            var customOperationParentIdPropertyName = "MyOperationParentId";
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(
                       _ => connectionString,
                       opt =>
                       {
                           opt.AutoComplete = true;
                           opt.Correlation.Format = MessageCorrelationFormat.Hierarchical;
                           opt.Correlation.OperationParentIdPropertyName = customOperationParentIdPropertyName;
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
            string connectionString = _config.GetServiceBusQueueConnectionString();
            ServiceBusConnectionStringProperties properties = ServiceBusConnectionStringProperties.Parse(connectionString);

            ServicePrincipal servicePrincipal = _config.GetServiceBusServicePrincipal();
            string tenantId = _config.GetTenantId();
            
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, tenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var options = new WorkerOptions();
                options.AddEventGridPublisher(_config)
                       .AddServiceBusQueueMessagePumpUsingManagedIdentity(
                           queueName: properties.EntityPath,
                           serviceBusNamespace: properties.FullyQualifiedNamespace,
                           clientId: servicePrincipal.ClientId,
                           configureMessagePump: opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

                // Act / Assert
                await TestServiceBusQueueMessageHandlingForW3CAsync(options);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithCustomCompleteOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(
                        _ => connectionString, 
                        opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
                   .WithServiceBusFallbackMessageHandler<OrdersFallbackCompleteMessageHandler>();
            
            // Act / Assert
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithBatchedMessages_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = true)
                    .WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(messageBodySerializerImplementationFactory: serviceProvider =>
                    {
                        var logger = serviceProvider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                        return new OrderBatchMessageBodySerializer(logger);
                    });
            
            // Act / Assert
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextTypeFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextAndBodyFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.ContainsKey("NotExisting"), _ => false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>(
                        context => context.Properties["Topic"].ToString() == "Orders", 
                        body => body.Id != null);

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);
            message.ApplicationProperties["Topic"] = "Orders";

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                OrderCreatedEventData eventData = consumer.ConsumeOrderEventForW3C(traceParent.TransactionId);
                AssertReceivedOrderEventDataForW3C(message, eventData, traceParent);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithContextFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(subscriptionName: Guid.NewGuid().ToString(), _ => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Customers")
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Orders")
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext _) => false);

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);
            message.ApplicationProperties["Topic"] = "Orders";

            var producer = TestServiceBusMessageProducer.CreateForTopic(_config);
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                OrderCreatedEventData eventData = consumer.ConsumeOrderEventForW3C(traceParent.TransactionId);
                AssertReceivedOrderEventDataForW3C(message, eventData, traceParent);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithBodyFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(subscriptionName: Guid.NewGuid().ToString(), _ => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>((Customer body) => body is null)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>((Order body) => body.Id != null)
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((Order _) => false);

            // Act / Assert
            await TestServiceBusTopicMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextAndBodyFilteringWithSerializer_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = true)
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
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
        }

        [Fact(Skip = ".NET application cannot start multiple blocking background tasks, see https://github.com/dotnet/runtime/issues/36063")]
        public async Task ServiceBusMessagePumpWithQueueAndTopic_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config);
            options.AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            options.AddServiceBusTopicMessagePump(
                subscriptionName: Guid.NewGuid().ToString(),
                _ => _config.GetServiceBusConnectionString(ServiceBusEntityType.Topic),
                opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            // Act / Assert
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
            await TestServiceBusTopicMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithFallbackMessageHandler<OrdersFallbackMessageHandler>();
            
            // Act / Assert
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithServiceBusFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithServiceBusFallbackMessageHandler<OrdersServiceBusFallbackMessageHandler>();

            // Act / Assert
            await TestServiceBusQueueMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetter_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusQueueConnectionString();
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => connectionString, opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusDeadLetterMessageHandler, Order>();

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);

            // Act
            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            var consumer = TestServiceBusDeadLetterMessageConsumer.CreateForQueue(_config, _logger);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                await consumer.AssertDeadLetterMessageAsync();
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetterOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
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
            var consumer = TestServiceBusDeadLetterMessageConsumer.CreateForQueue(_config, _logger);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                await consumer.AssertDeadLetterMessageAsync();
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
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusDeadLetterMessageHandler, Order>(context => context.Properties["Topic"].ToString() == "Orders")
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext _) => false);

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);

            var producer = TestServiceBusMessageProducer.CreateForQueue(_config);
            var consumer = TestServiceBusDeadLetterMessageConsumer.CreateForQueue(_config, _logger);
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                await consumer.AssertDeadLetterMessageAsync();
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithServiceBusAbandon_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: Guid.NewGuid().ToString(), 
                       _ => connectionString,
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusAbandonMessageHandler, Order>();
            
            // Act / Assert
            await TestServiceBusTopicMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithServiceBusAbandonOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: Guid.NewGuid().ToString(), 
                       _ => connectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                   .WithServiceBusFallbackMessageHandler<OrdersAzureServiceBusAbandonFallbackMessageHandler>();
            
            // Act / Assert
            await TestServiceBusTopicMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithServiceBusAbandonAfterContextPredicate_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: Guid.NewGuid().ToString(), 
                       _ => connectionString, 
                       options => options.AutoComplete = false)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusAbandonMessageHandler, Order>((AzureServiceBusMessageContext _) => true);
            
            // Act / Assert
            await TestServiceBusTopicMessageHandlingForW3CAsync(options);
        }

        [Fact]
        public async Task ServiceBusMessagePump_PauseViaLifetime_RestartsAgain()
        {
            // Arrange
            string connectionString = _config.GetServiceBusTopicConnectionString();
            string jobId = Guid.NewGuid().ToString();
            var options = new WorkerOptions();
            options.AddEventGridPublisher(_config)
                   .AddServiceBusTopicMessagePump(
                       subscriptionName: Guid.NewGuid().ToString(), 
                       _ => connectionString, 
                       opt => opt.JobId = jobId)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusAbandonMessageHandler, Order>((AzureServiceBusMessageContext _) => true);

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);

            var producer = TestServiceBusMessageProducer.CreateFor(_config, ServiceBusEntityType.Topic);
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                var lifetime = worker.Services.GetRequiredService<IMessagePumpLifetime>();
                await lifetime.PauseProcessingMessagesAsync(jobId, TimeSpan.FromSeconds(5), CancellationToken.None);

                // Act
                await producer.ProduceAsync(message);

                // Assert
                OrderCreatedEventData eventData = consumer.ConsumeOrderEventForW3C(traceParent.TransactionId);
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
                AssertX.RetryAssertUntilTelemetryShouldBeAvailable(() =>
                {
                    RequestTelemetry requestViaArcusServiceBus = AssertX.GetRequestFrom(spySink.Telemetries, r => r.Name == operationName && r.Context.Operation.Id == traceParent.TransactionId);
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
                AssertX.RetryAssertUntilTelemetryShouldBeAvailable(() =>
                {
                    RequestTelemetry requestViaArcusServiceBus = AssertX.GetRequestFrom(spySink.Telemetries, r => r.Name == operationName);
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
                       opt.Correlation.Format = MessageCorrelationFormat.Hierarchical;
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
                AssertX.RetryAssertUntilTelemetryShouldBeAvailable(() =>
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
            options.AddEventGridPublisher(_config)
                   .AddSecretStore(stores => stores.AddAzureKeyVaultWithServicePrincipal(
                       rawVaultUri: keyRotationConfig.KeyVault.VaultUri,
                       tenantId: tenantId,
                       clientId: keyRotationConfig.ServicePrincipal.ClientId,
                       clientKey: keyRotationConfig.ServicePrincipal.ClientSecret,
                       cacheConfiguration: CacheConfiguration.Default))
                   .AddServiceBusQueueMessagePump(keyRotationConfig.KeyVault.SecretName, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                string newSecondaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.SecondaryKey);
                await SetConnectionStringInKeyVaultAsync(secretClient, keyRotationConfig, newSecondaryConnectionString);

                await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
                {
                    // Act
                    string newPrimaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

                    // Assert
                    var producer = new TestServiceBusMessageProducer(newPrimaryConnectionString);
                    await producer.ProduceAsync(message);

                    OrderCreatedEventData eventData = consumer.ConsumeOrderEventForW3C(traceParent.TransactionId);
                    AssertReceivedOrderEventDataForW3C(message, eventData, traceParent);
                }
            }
        }

        private async Task TestServiceBusQueueMessageHandlingForHierarchicalAsync(WorkerOptions options)
        {
            await TestServiceBusMessageHandlingForHierarchicalAsync(options, ServiceBusEntityType.Queue);
        }

        private async Task TestServiceBusMessageHandlingForHierarchicalAsync(WorkerOptions options, ServiceBusEntityType entityType)
        {
            ServiceBusMessage message = CreateOrderServiceBusMessageForHierarchical();

            var producer = TestServiceBusMessageProducer.CreateFor(_config, entityType);
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                OrderCreatedEventData eventData = consumer.ConsumeOrderEventForHierarchical(message.CorrelationId);
                AssertReceivedOrderEventDataForHierarchical(message, eventData);
            }
        }

        private async Task TestServiceBusTopicMessageHandlingForW3CAsync(WorkerOptions options)
        {
            await TestServiceBusMessageHandlingForW3CAsync(options, ServiceBusEntityType.Topic);
        }

        private async Task TestServiceBusQueueMessageHandlingForW3CAsync(WorkerOptions options)
        {
            await TestServiceBusMessageHandlingForW3CAsync(options, ServiceBusEntityType.Queue);
        }

        private async Task TestServiceBusMessageHandlingForW3CAsync(
            WorkerOptions options,
            ServiceBusEntityType entityType)
        {
            options.AddXunitTestLogging(_outputWriter);
            var traceParent = TraceParent.Generate();
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(traceParent);

            var producer = TestServiceBusMessageProducer.CreateFor(_config, entityType);
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(_config, _logger))
            {
                // Act
                await producer.ProduceAsync(message);

                // Assert
                OrderCreatedEventData eventData = consumer.ConsumeOrderEventForW3C(traceParent.TransactionId);
                AssertReceivedOrderEventDataForW3C(message, eventData, traceParent);
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