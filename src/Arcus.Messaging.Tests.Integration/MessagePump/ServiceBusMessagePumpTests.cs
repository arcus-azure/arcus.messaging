﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageBodyHandlers;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Observability.Telemetry.Core;
using Arcus.Security.Core;
using Arcus.Security.Providers.AzureKeyVault;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Security.Providers.AzureKeyVault.Configuration;
using Arcus.Testing.Logging;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using RetryPolicy = Polly.Retry.RetryPolicy;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Collection("Integration")]
    [Trait("Category", "Integration")]
    public class ServiceBusMessagePumpTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessagePumpTests"/> class.
        /// </summary>
        public ServiceBusMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithNamespaceScopedConnectionString_PublishesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string entityConnectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var properties = ServiceBusConnectionStringProperties.Parse(entityConnectionString);
            string namespaceConnectionString = properties.GetNamespaceConnectionString();

            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(properties.EntityPath, configuration => namespaceConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(entityConnectionString);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithEntityScopedConnectionString_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump(
                        "Test-Receive-All-Topic-Only", 
                        configuration => connectionString, 
                        opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }
        
        [Fact]
        public async Task ServiceBusTopicMessagePumpWithSubscriptionNameOver50_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump(
                       "Test-Receive-All-Topic-Only-with-an-azure-servicebus-topic-subscription-name-over-50-characters", 
                       configuration => connectionString, 
                       opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithNamespaceScopedConnectionString_PublishesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string topicConnectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Topic);
            var properties = ServiceBusConnectionStringProperties.Parse(topicConnectionString);
            string namespaceConnectionString = properties.GetNamespaceConnectionString();

            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump(
                       topicName: properties.EntityPath,
                       subscriptionName: "Test-Receive-All-Topic-Only",
                       getConnectionStringFromConfigurationFunc: configuration => namespaceConnectionString,
                       configureMessagePump: opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(topicConnectionString);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithIgnoringMissingMembersDeserialization_PublishesServiceBusMessage_MessageGetsProcessedByDifferentMessageHandler()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);

            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore)
                   .WithServiceBusMessageHandler<OrderV2AzureServiceBusMessageHandler, OrderV2>();

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpUsingManagedIdentity_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Topic);
            ServiceBusConnectionStringProperties properties = ServiceBusConnectionStringProperties.Parse(connectionString);

            ServicePrincipal servicePrincipal = config.GetServiceBusServicePrincipal();
            string tenantId = config.GetTenantId();

            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, tenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var options = new WorkerOptions();
                options.AddEventGridPublisher(config)
                       .AddServiceBusTopicMessagePumpUsingManagedIdentity(
                           topicName: properties.EntityPath,
                           subscriptionName: "Test-Receive-All-Topic-Only", 
                           serviceBusNamespace: properties.FullyQualifiedNamespace,
                           clientId: servicePrincipal.ClientId,
                           configureMessagePump: opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

                // Act
                await using (var worker = await Worker.StartNewAsync(options))
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Assert
                    await service.SimulateMessageProcessingAsync(connectionString);
                }
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithCustomComplete_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump(
                       "Test-Receive-All-Topic-Only", 
                       configuration => connectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusCompleteMessageHandler, Order>();
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithCustomCompleteOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(
                       configuration => connectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                   .WithServiceBusFallbackMessageHandler<OrdersFallbackCompleteMessageHandler>();
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpUsingManagedIdentity_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            ServiceBusConnectionStringProperties properties = ServiceBusConnectionStringProperties.Parse(connectionString);

            ServicePrincipal servicePrincipal = config.GetServiceBusServicePrincipal();
            string tenantId = config.GetTenantId();
            
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, tenantId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId))
            using (TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret))
            {
                var options = new WorkerOptions();
                options.AddEventGridPublisher(config)
                       .AddServiceBusQueueMessagePumpUsingManagedIdentity(
                           queueName: properties.EntityPath,
                           serviceBusNamespace: properties.FullyQualifiedNamespace,
                           clientId: servicePrincipal.ClientId,
                           configureMessagePump: opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

                // Act
                await using (var worker = await Worker.StartNewAsync(options))
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Assert
                    await service.SimulateMessageProcessingAsync(connectionString);
                }
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithCustomCompleteOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(
                        configuration => connectionString, 
                        opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
                   .WithServiceBusFallbackMessageHandler<OrdersFallbackCompleteMessageHandler>();
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithBatchedMessages_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                    .WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(messageBodySerializerImplementationFactory: serviceProvider =>
                    {
                        var logger = serviceProvider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                        return new OrderBatchMessageBodySerializer(logger);
                    });
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextTypeFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextAndBodyFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.ContainsKey("NotExisting"), body => false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>(
                        context => context.Properties["Topic"].ToString() == "Orders", 
                        body => body.Id != null);

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithContextFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump("Test-Receive-All-Topic-Only", configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Customers")
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Orders")
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext context) => false);

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithBodyFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump("Test-Receive-All-Topic-Only", configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>((Customer body) => body is null)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>((Order body) => body.Id != null)
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((Order body) => false);

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextAndBodyFilteringWithSerializer_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>(messageContextFilter: context => false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(messageBodyFilter: message => true)
                   .WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(
                       messageContextFilter: context => context != null,
                       messageBodySerializerImplementationFactory: serviceProvider =>
                       {
                           var logger = serviceProvider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                           return new OrderBatchMessageBodySerializer(logger);
                       },
                       messageBodyFilter: message => message.Orders.Length == 1);

            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithQueueAndTopic_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true);
            options.AddServiceBusTopicMessagePump(
                       "Test-Receive-All-Topic-And-Queue", 
                       configuration => config.GetServiceBusConnectionString(ServiceBusEntityType.Topic), 
                       opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Act / Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext context) => false)
                   .WithFallbackMessageHandler<OrdersFallbackMessageHandler>();
            
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Act / Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithServiceBusFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext context) => false)
                   .WithServiceBusFallbackMessageHandler<OrdersServiceBusFallbackMessageHandler>();

            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Act / Assert
                await service.SimulateMessageProcessingAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetter_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusDeadLetterMessageHandler, Order>();
            
            Order order = OrderGenerator.Generate();

            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Act
                await service.SendMessageToServiceBusAsync(connectionString, order.AsServiceBusMessage());

                // Assert
                await service.AssertDeadLetterMessageAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetterOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(
                       configuration => connectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>((AzureServiceBusMessageContext context) => true)
                   .WithServiceBusFallbackMessageHandler<OrdersAzureServiceBusDeadLetterFallbackMessageHandler>();
            
            Order order = OrderGenerator.Generate();

            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Act
                await service.SendMessageToServiceBusAsync(connectionString, order.AsServiceBusMessage());

                // Assert
                await service.AssertDeadLetterMessageAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetterAfterContextPredicate_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties["Topic"].ToString() == "Customers")
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusDeadLetterMessageHandler, Order>(context => context.Properties["Topic"].ToString() == "Orders")
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext context) => false);

            Order order = OrderGenerator.Generate();

            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Act
                await service.SendMessageToServiceBusAsync(connectionString, order.AsServiceBusMessage());

                // Assert
                await service.AssertDeadLetterMessageAsync(connectionString);
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithServiceBusAbandon_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump("Test-Receive-All-Topic-Only",
                       configuration => connectionString,
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusAbandonMessageHandler, Order>();
            
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Act
                await service.SimulateMessageProcessingAsync(connectionString);
            } 
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithServiceBusAbandonOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump(
                       "Test-Receive-All-Topic-Only", 
                       configuration => connectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                   .WithServiceBusFallbackMessageHandler<OrdersAzureServiceBusAbandonFallbackMessageHandler>();
            
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Act
                await service.SimulateMessageProcessingAsync(connectionString);
            } 
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithServiceBusAbandonAfterContextPredicate_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump(
                       "Test-Receive-All-Topic-Only", 
                       configuration => connectionString, 
                       options => options.AutoComplete = false)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext context) => false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusAbandonMessageHandler, Order>((AzureServiceBusMessageContext context) => true);
            
            await using (var worker = await Worker.StartNewAsync(options))
            await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
            {
                // Act
                await service.SimulateMessageProcessingAsync(connectionString);
            } 
        }

        [Fact]
        public async Task ServiceBusMessagePump_FailureDuringMessageHandling_TracksCorrelationInApplicationInsights()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntityType.Queue);

            var spySink = new InMemoryLogSink();
            var options = new WorkerOptions();
            options.Configure(host => host.UseSerilog((context, currentConfig) =>
            {
                currentConfig
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .Enrich.WithVersion()
                    .Enrich.WithComponentName("Service Bus Queue Worker")
                    .WriteTo.Sink(spySink);
            }));
            options.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersSabotageAzureServiceBusMessageHandler, Order>();
            
            string operationId = $"operation-{Guid.NewGuid()}", transactionId = $"transaction-{Guid.NewGuid()}";
            ServiceBusMessage orderMessage = OrderGenerator.Generate().AsServiceBusMessage(operationId, transactionId);

            await using (var worker = await Worker.StartNewAsync(options))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act
                    await service.SendMessageToServiceBusAsync(connectionString, orderMessage);
                }
            
                // Assert
                RetryAssertUntilTelemetryShouldBeAvailable(() =>
                {
                    Assert.Contains(spySink.CurrentLogEmits,
                        log => log.Exception?.InnerException?.Message.Contains("Sabotage") == true &&
                               log.ContainsProperty(ContextProperties.Correlation.OperationId, operationId) &&
                               log.ContainsProperty(ContextProperties.Correlation.TransactionId, transactionId));
                },
                timeout: TimeSpan.FromMinutes(1));
            }
        }

        [Fact]
        public async Task ServiceBusMessagePump_RotateServiceBusConnectionKeys_MessagePumpRestartsThenMessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            KeyRotationConfig keyRotationConfig = config.GetKeyRotationConfig();
            _logger.LogInformation("Using Service Principal [ClientID: '{ClientId}']", keyRotationConfig.ServicePrincipal.ClientId);

            var client = new ServiceBusConfiguration(keyRotationConfig, _logger);
            string freshConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

            ServicePrincipalAuthentication authentication = keyRotationConfig.ServicePrincipal.CreateAuthentication();
            IKeyVaultClient keyVaultClient = await authentication.AuthenticateAsync();
            await SetConnectionStringInKeyVaultAsync(keyVaultClient, keyRotationConfig, freshConnectionString);

            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddSingleton<ISecretProvider>(serviceProvider =>
                   {
                       return new KeyVaultSecretProvider(
                           new ServicePrincipalAuthentication(keyRotationConfig.ServicePrincipal.ClientId,
                               keyRotationConfig.ServicePrincipal.ClientSecret),
                           new KeyVaultConfiguration(keyRotationConfig.KeyVault.VaultUri));
                   })
                   .AddServiceBusQueueMessagePump(keyRotationConfig.KeyVault.SecretName, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            await using (var worker = await Worker.StartNewAsync(options))
            {
                string newSecondaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.SecondaryKey);
                await SetConnectionStringInKeyVaultAsync(keyVaultClient, keyRotationConfig, newSecondaryConnectionString);

                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act
                    string newPrimaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

                    // Assert
                    await service.SimulateMessageProcessingAsync(newPrimaryConnectionString);
                }
            }
        }

        private static async Task SetConnectionStringInKeyVaultAsync(IKeyVaultClient keyVaultClient, KeyRotationConfig keyRotationConfig, string rotatedConnectionString)
        {
            await keyVaultClient.SetSecretAsync(
                vaultBaseUrl: keyRotationConfig.KeyVault.VaultUri,
                secretName: keyRotationConfig.KeyVault.SecretName,
                value: rotatedConnectionString);
        }

        private void RetryAssertUntilTelemetryShouldBeAvailable(System.Action assertion, TimeSpan timeout)
        {
            RetryPolicy retryPolicy =
                Policy.Handle<Exception>(exception =>
                      {
                          _logger.LogError(exception, "Failed assertion. Reason: {Message}", exception.Message);
                          return true;
                      })
                      .WaitAndRetryForever(index => TimeSpan.FromSeconds(3));

            Policy.Timeout(timeout)
                  .Wrap(retryPolicy)
                  .Execute(assertion);
        }
    }
}