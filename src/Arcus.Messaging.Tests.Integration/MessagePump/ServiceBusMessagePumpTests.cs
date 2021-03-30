using System;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.KeyRotation.Extensions;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
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
using Microsoft.Azure.ApplicationInsights.Query;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using RetryPolicy = Polly.Retry.RetryPolicy;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
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
        public async Task ServiceBusQueueMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
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
        public async Task ServiceBusTopicMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Topic);
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
        public async Task ServiceBusTopicMessagePumpWithCustomComplete_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Topic);
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
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
        public async Task ServiceBusQueueMessagePumpWithCustomCompleteOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, options => options.AutoComplete = true)
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump("Test-Receive-All-Topic-Only", configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext context) => false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Customers")
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Orders");

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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump("Test-Receive-All-Topic-Only", configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((Order body) => false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>((Customer body) => body is null)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>((Order body) => body.Id != null);

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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>(messageContextFilter: context => false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(messageBodyFilter: message => false)
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .AddServiceBusTopicMessagePump(
                       "Test-Receive-All-Topic-And-Queue", 
                       configuration => config.GetServiceBusConnectionString(ServiceBusEntity.Topic), 
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, options => options.AutoComplete = true)
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusQueueMessagePump(configuration => connectionString, options => options.AutoComplete = true)
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(
                        configuration => connectionString, 
                        options => options.AutoComplete = false)
                    .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = false)
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext context) => false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties["Topic"].ToString() == "Customers")
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusDeadLetterMessageHandler, Order>(context => context.Properties["Topic"].ToString() == "Orders");

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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Topic);
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Topic);
            var options = new WorkerOptions();
            options.AddEventGridPublisher(config)
                   .AddServiceBusTopicMessagePump(
                       "Test-Receive-All-Topic-Only", 
                       configuration => connectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Topic);
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);

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
            Message orderMessage = OrderGenerator.Generate().AsServiceBusMessage(operationId, transactionId);

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

        [Fact]
        public async Task ServiceBusMessagePump_RotateServiceBusConnectionKeysOnSecretNewVersionNotification_MessagePumpRestartsThenMessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            KeyRotationConfig rotationConfig = config.GetKeyRotationConfig();
            _logger.LogInformation("Using Service Principal [ClientID: '{0}']", rotationConfig.ServicePrincipal.ClientId);

            var client = new ServiceBusConfiguration(rotationConfig, _logger);
            string freshConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

            IKeyVaultClient keyVaultClient = await CreateKeyVaultClientAsync(rotationConfig);
            await SetConnectionStringInKeyVaultAsync(keyVaultClient, rotationConfig, freshConnectionString);

            string jobId = Guid.NewGuid().ToString();
            const string connectionStringSecretKey = "ARCUS_KEYVAULT_SECRETNEWVERSIONCREATED_CONNECTIONSTRING";

            var options = new WorkerOptions();
            options.Configuration.Add(connectionStringSecretKey, rotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString);
            options.AddEventGridPublisher(config);
            options.Configure(host => host.ConfigureSecretStore((configuration, stores) =>
            {
                stores.AddAzureKeyVaultWithServicePrincipal(
                          rotationConfig.KeyVault.VaultUri,
                          rotationConfig.ServicePrincipal.ClientId,
                          rotationConfig.ServicePrincipal.ClientSecret)
                      .AddConfiguration(configuration);
            })).AddServiceBusQueueMessagePump(rotationConfig.KeyVault.SecretName, opt => 
            {
                opt.JobId = jobId;
                // Unrealistic big maximum exception count so that we're certain that the message pump gets restarted based on the notification and not the unauthorized exception.
                opt.MaximumUnauthorizedExceptionsBeforeRestart = 1000;
            }).WithAutoRestartServiceBusMessagePumpOnRotatedCredentials(
                jobId: jobId,
                subscriptionNamePrefix: "TestSub",
                serviceBusTopicConnectionStringSecretKey: connectionStringSecretKey,
                messagePumpConnectionStringKey: rotationConfig.KeyVault.SecretName)
            .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            await using (var worker = await Worker.StartNewAsync(options))
            {
                string newSecondaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.SecondaryKey);
                await SetConnectionStringInKeyVaultAsync(keyVaultClient, rotationConfig, newSecondaryConnectionString);

                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act
                    string newPrimaryConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

                    // Assert
                    await service.SimulateMessageProcessingAsync(newPrimaryConnectionString);
                }
            }
        }

        private static async Task<IKeyVaultClient> CreateKeyVaultClientAsync(KeyRotationConfig rotationConfig)
        {
            ServicePrincipalAuthentication authentication = rotationConfig.ServicePrincipal.CreateAuthentication();
            IKeyVaultClient keyVaultClient = await authentication.AuthenticateAsync();
            
            return keyVaultClient;
        }

        private static async Task SetConnectionStringInKeyVaultAsync(IKeyVaultClient keyVaultClient, KeyRotationConfig keyRotationConfig, string rotatedConnectionString)
        {
            await keyVaultClient.SetSecretAsync(
                vaultBaseUrl: keyRotationConfig.KeyVault.VaultUri,
                secretName: keyRotationConfig.KeyVault.SecretName,
                value: rotatedConnectionString);
        }

        private static ApplicationInsightsDataClient CreateApplicationInsightsClient(string instrumentationKey)
        {
            var clientCredentials = new ApiKeyClientCredentials(instrumentationKey);
            var client = new ApplicationInsightsDataClient(clientCredentials);

            return client;
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
