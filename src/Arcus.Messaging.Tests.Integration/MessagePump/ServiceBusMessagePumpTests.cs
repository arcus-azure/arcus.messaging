using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.ServiceBus;
using Arcus.Messaging.Tests.Workers.ServiceBus;
using Arcus.Observability.Telemetry.Core;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Testing.Logging;
using Microsoft.Azure.ApplicationInsights.Query;
using Microsoft.Azure.ApplicationInsights.Query.Models;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Azure.ServiceBus;
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

        [Theory]
        [InlineData(ServiceBusEntity.Queue, typeof(ServiceBusQueueProgram))]
        [InlineData(ServiceBusEntity.Queue, typeof(ServiceBusQueueContextTypeSelectionProgram))]
        [InlineData(ServiceBusEntity.Queue, typeof(ServiceBusQueueContextAndBodyFilterSelectionProgram))]
        [InlineData(ServiceBusEntity.Topic, typeof(ServiceBusTopicProgram))]
        [InlineData(ServiceBusEntity.Topic, typeof(ServiceBusTopicContextPredicateSelectionProgram))]
        [InlineData(ServiceBusEntity.Topic, typeof(ServiceBusTopicBodyPredicateSelectionProgram))]
        [InlineData(ServiceBusEntity.Queue, typeof(ServiceBusQueueCompleteProgram))]
        [InlineData(ServiceBusEntity.Topic, typeof(ServiceBusTopicFallbackCompleteProgram))]
        [InlineData(ServiceBusEntity.Topic, typeof(ServiceBusTopicWithOrderBatchProgram))]
        [InlineData(ServiceBusEntity.Queue, typeof(ServiceBusQueueWithContextAndBodyFilterAndBodySerializerProgram))]
        public async Task ServiceBusMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed(ServiceBusEntity entity, Type programType)
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(entity);
            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", connectionString),
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync(programType, config, _logger, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act / Assert
                    await service.SimulateMessageProcessingAsync(connectionString);
                }
            }
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithQueueAndTopic_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            
            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING_WITH_QUEUE", connectionString),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING_WITH_TOPIC", config.GetServiceBusConnectionString(ServiceBusEntity.Topic)),
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync<ServiceBusQueueAndTopicProgram>(config, _logger, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act / Assert
                    await service.SimulateMessageProcessingAsync(connectionString);
                }
            }
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);

            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", connectionString),
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync<ServiceBusQueueWithFallbackProgram>(config, _logger, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act / Assert
                    await service.SimulateMessageProcessingAsync(connectionString);
                }
            }
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithServiceBusFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);

            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", connectionString),
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync<ServiceBusQueueWithServiceBusFallbackProgram>(config, _logger, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act / Assert
                    await service.SimulateMessageProcessingAsync(connectionString);
                }
            }
        }

        [Theory]
        [InlineData(typeof(ServiceBusQueueWithServiceBusDeadLetterProgram))]
        [InlineData(typeof(ServiceBusQueueWithServiceBusDeadLetterFallbackProgram))]
        [InlineData(typeof(ServiceBusQueueContextPredicateSelectionWithDeadLetterProgram))]
        public async Task ServiceBusMessagePumpWithServiceBusDeadLetter_PublishServiceBusMessage_MessageSuccessfullyProcessed(Type programType)
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Queue);
            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", connectionString),
            };

            Order order = OrderGenerator.Generate();

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync(programType, config, _logger, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act
                    await service.SendMessageToServiceBusAsync(connectionString, order.AsServiceBusMessage());

                    // Assert
                    await service.AssertDeadLetterMessageAsync(connectionString);
                }
            }
        }

        [Theory]
        [InlineData(typeof(ServiceBusTopicWithServiceBusAbandonProgram))]
        [InlineData(typeof(ServiceBusTopicWithServiceBusAbandonFallbackProgram))]
        [InlineData(typeof(ServiceBusTopicContextPredicateSelectionWithServiceBusAbandonProgram))]
        public async Task ServiceBusMessagePumpWithServiceBusAbandon_PublishServiceBusMessage_MessageSuccessfullyProcessed(Type programType)
        {
            // Arrange
            var config = TestConfig.Create();
            string connectionString = config.GetServiceBusConnectionString(ServiceBusEntity.Topic);
            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", connectionString)
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync(programType, config, _logger, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _logger))
                {
                    // Act
                    await service.SimulateMessageProcessingAsync(connectionString);
                }
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

            using var project = await ServiceBusWorkerProject.StartNewWithAsync<ServiceBusQueueTrackCorrelationOnExceptionProgram>(config, _logger, commandArguments);
            await using var service = await TestMessagePumpService.StartNewAsync(config, _logger);
            Message orderMessage = OrderGenerator.Generate().AsServiceBusMessage(operationId, transactionId);
                    
            // Act
            await service.SendMessageToServiceBusAsync(connectionString, orderMessage);

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
                }, timeout: TimeSpan.FromMinutes(5));
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
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_VAULTURI", keyRotationConfig.KeyVaultSecret.VaultUri), 
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_CONNECTIONSTRINGSECRETNAME", keyRotationConfig.KeyVaultSecret.SecretName),
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_SERVICEPRINCIPAL_CLIENTID", keyRotationConfig.ServicePrincipal.ClientId), 
                CommandArgument.CreateSecret("ARCUS_KEYVAULT_SERVICEPRINCIPAL_CLIENTSECRET", keyRotationConfig.ServicePrincipal.ClientSecret), 
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync<ServiceBusQueueKeyVaultProgram>(config, _logger, commandArguments))
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
                vaultBaseUrl: keyRotationConfig.KeyVaultSecret.VaultUri,
                secretName: keyRotationConfig.KeyVaultSecret.SecretName,
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
                      .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(1));

            await Policy.TimeoutAsync(timeout)
                        .WrapAsync(retryPolicy)
                        .ExecuteAsync(assertion);
        }
    }
}
