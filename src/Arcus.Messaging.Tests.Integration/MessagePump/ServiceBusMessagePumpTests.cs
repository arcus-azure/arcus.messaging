using System;
using System.Threading.Tasks;
using Arcus.EventGrid.Publishing;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageBodyHandlers;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Messaging.Tests.Workers.ServiceBus;
using Arcus.Observability.Telemetry.Core;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Testing.Logging;
using Microsoft.Azure.ApplicationInsights.Query;
using Microsoft.Azure.ApplicationInsights.Query.Models;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;
using Xunit.Abstractions;
using RetryPolicy = Polly.Retry.RetryPolicy;

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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            // Act
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusTopicMessagePump(
                        "Test-Receive-All-Topic-Only", 
                        configuration => connectionString, 
                        opt => opt.AutoComplete = true)
                    .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            // Act
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusTopicMessagePump(
                       "Test-Receive-All-Topic-Only", 
                       configuration => connectionString, 
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusCompleteMessageHandler, Order>();
            
            // Act
            await using (var worker = Worker.StartNew(options))
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
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Topic);
            var options = new WorkerOptions();
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusQueueMessagePump(
                        configuration => connectionString, 
                        opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
                   .WithServiceBusFallbackMessageHandler<OrdersFallbackCompleteMessageHandler>();
            
            // Act
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusQueueMessagePump(
                        configuration => connectionString, 
                        opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
                   .WithServiceBusFallbackMessageHandler<OrdersFallbackCompleteMessageHandler>();
            
            // Act
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusQueueMessagePump(configuration => connectionString, options => options.AutoComplete = true)
                    .WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(messageBodySerializerImplementationFactory: serviceProvider =>
                    {
                        var logger = serviceProvider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                        return new OrderBatchMessageBodySerializer(logger);
                    });
            
            // Act
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();

            // Act
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.ContainsKey("NotExisting"), body => false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>(
                        context => context.Properties["Topic"].ToString() == "Orders", 
                        body => body.Id != null);

            // Act
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusTopicMessagePump("Test-Receive-All-Topic-Only", configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext context) => false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Customers")
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Orders");

            // Act
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusTopicMessagePump("Test-Receive-All-Topic-Only", configuration => connectionString, opt => opt.AutoComplete = true)
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((Order body) => false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>((Customer body) => body is null)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>((Order body) => body.Id != null);

            // Act
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();
                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
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
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();

                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = true)
                    .AddServiceBusTopicMessagePump(
                        "Test-Receive-All-Topic-And-Queue", 
                        configuration => config.GetServiceBusConnectionString(ServiceBusEntity.Topic), 
                        opt => opt.AutoComplete = true)
                    .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
            
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();

                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusQueueMessagePump(configuration => connectionString, options => options.AutoComplete = true)
                    .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext context) => false)
                    .WithFallbackMessageHandler<OrdersFallbackMessageHandler>();
            
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();

                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusQueueMessagePump(configuration => connectionString, options => options.AutoComplete = true)
                    .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext context) => false)
                    .WithServiceBusFallbackMessageHandler<OrdersServiceBusFallbackMessageHandler>();

            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusDeadLetterMessageHandler, Order>();
            
            Order order = OrderGenerator.Generate();

            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddServiceBusQueueMessagePump(
                        configuration => connectionString, 
                        options => options.AutoComplete = false)
                    .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
                    .WithServiceBusFallbackMessageHandler<OrdersAzureServiceBusDeadLetterFallbackMessageHandler>();
            
            Order order = OrderGenerator.Generate();

            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddServiceBusQueueMessagePump(configuration => connectionString, opt => opt.AutoComplete = false)
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext context) => false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties["Topic"].ToString() == "Customers")
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusDeadLetterMessageHandler, Order>(context => context.Properties["Topic"].ToString() == "Orders");

            Order order = OrderGenerator.Generate();

            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();

                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusTopicMessagePump("Test-Receive-All-Topic-Only",
                       configuration => connectionString,
                       opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusAbandonMessageHandler, Order>();
            
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();

                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusTopicMessagePump(
                        "Test-Receive-All-Topic-Only", 
                        configuration => connectionString, 
                        options => options.AutoComplete = false)
                    .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
                    .WithServiceBusFallbackMessageHandler<OrdersAzureServiceBusAbandonFallbackMessageHandler>();
            
            await using (var worker = Worker.StartNew(options))
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
            options.Services.AddTransient(svc =>
            {
                string eventGridTopic = config.GetTestInfraEventGridTopicUri();
                string eventGridKey = config.GetTestInfraEventGridAuthKey();

                return EventGridPublisherBuilder
                       .ForTopic(eventGridTopic)
                       .UsingAuthenticationKey(eventGridKey)
                       .Build();
            });
            options.Services.AddServiceBusTopicMessagePump(
                       "Test-Receive-All-Topic-Only", 
                       configuration => configuration["ARCUS_SERVICEBUS_CONNECTIONSTRING"], 
                       options => options.AutoComplete = false)
                   .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext context) => false)
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusAbandonMessageHandler, Order>((AzureServiceBusMessageContext context) => true);
            
            await using (var worker = Worker.StartNew(options))
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
            string operationId = $"operation-{Guid.NewGuid()}", transactionId = $"transaction-{Guid.NewGuid()}";

            var config = TestConfig.Create();
            ApplicationInsightsConfig applicationInsightsConfig = config.GetApplicationInsightsConfig();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("APPLICATIONINSIGHTS_INSTRUMENTATIONKEY", applicationInsightsConfig.InstrumentationKey),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", connectionString)
            };

            Message orderMessage = OrderGenerator.Generate().AsServiceBusMessage(operationId, transactionId);

            using (var project = await WorkerProject.StartNewWithAsync<ServiceBusQueueTrackCorrelationOnExceptionProgram>(config, _logger, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act
                    await service.SendMessageToServiceBusAsync(connectionString, orderMessage);
                }
            
                // Assert
                using (ApplicationInsightsDataClient client = CreateApplicationInsightsClient(applicationInsightsConfig.ApiKey))
                {
                    await RetryAssertUntilTelemetryShouldBeAvailableAsync(async () =>
                    {
                        const string onlyLastHourFilter = "timestamp gt now() sub duration'PT1H'";
                        EventsResults<EventsExceptionResult> results = 
                            await client.Events.GetExceptionEventsAsync(applicationInsightsConfig.ApplicationId, filter: onlyLastHourFilter);

                        Assert.Contains(results.Value, result =>
                        {
                            result.CustomDimensions.TryGetValue(ContextProperties.Correlation.TransactionId, out string actualTransactionId);
                            result.CustomDimensions.TryGetValue(ContextProperties.Correlation.OperationId, out string actualOperationId);

                            return transactionId == actualTransactionId && operationId == actualOperationId && operationId == result.Operation.Id;
                        });
                    }, timeout: TimeSpan.FromMinutes(7));
                }
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

            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_VAULTURI", keyRotationConfig.KeyVault.VaultUri), 
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_CONNECTIONSTRINGSECRETNAME", keyRotationConfig.KeyVault.SecretName),
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_SECRETNEWVERSIONCREATED_CONNECTIONSTRING", keyRotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString), 
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_SERVICEPRINCIPAL_CLIENTID", keyRotationConfig.ServicePrincipal.ClientId), 
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_SERVICEPRINCIPAL_CLIENTSECRET", keyRotationConfig.ServicePrincipal.ClientSecret), 
            };

            using (var project = await WorkerProject.StartNewWithAsync<ServiceBusQueueKeyVaultProgram>(config, _logger, commandArguments))
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
            KeyRotationConfig keyRotationConfig = config.GetKeyRotationConfig();
            _logger.LogInformation("Using Service Principal [ClientID: '{0}']", keyRotationConfig.ServicePrincipal.ClientId);

            var client = new ServiceBusConfiguration(keyRotationConfig, _logger);
            string freshConnectionString = await client.RotateConnectionStringKeysForQueueAsync(KeyType.PrimaryKey);

            ServicePrincipalAuthentication authentication = keyRotationConfig.ServicePrincipal.CreateAuthentication();
            IKeyVaultClient keyVaultClient = await authentication.AuthenticateAsync();
            await SetConnectionStringInKeyVaultAsync(keyVaultClient, keyRotationConfig, freshConnectionString);

            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_VAULTURI", keyRotationConfig.KeyVault.VaultUri),
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_CONNECTIONSTRINGSECRETNAME", keyRotationConfig.KeyVault.SecretName),
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_SERVICEPRINCIPAL_CLIENTID", keyRotationConfig.ServicePrincipal.ClientId),
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_SERVICEPRINCIPAL_CLIENTSECRET", keyRotationConfig.ServicePrincipal.ClientSecret),
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_SECRETNEWVERSIONCREATED_CONNECTIONSTRING", keyRotationConfig.KeyVault.SecretNewVersionCreated.ConnectionString)
            };

            using (var project = await WorkerProject.StartNewWithAsync<ServiceBusQueueSecretNewVersionReAuthenticateProgram>(config, _logger, commandArguments))
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

        private static ApplicationInsightsDataClient CreateApplicationInsightsClient(string instrumentationKey)
        {
            var clientCredentials = new ApiKeyClientCredentials(instrumentationKey);
            var client = new ApplicationInsightsDataClient(clientCredentials);

            return client;
        }

        private async Task RetryAssertUntilTelemetryShouldBeAvailableAsync(Func<Task> assertion, TimeSpan timeout)
        {
            RetryPolicy retryPolicy =
                Policy.Handle<Exception>(exception =>
                      {
                          _logger.LogError(exception, "Failed to contact Azure Application Insights. Reason: {Message}", exception.Message);
                          return true;
                      })
                      .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(3));

            await Policy.TimeoutAsync(timeout)
                        .WrapAsync(retryPolicy)
                        .ExecuteAsync(assertion);
        }
    }
}
