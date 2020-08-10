using System;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.ServiceBus;
using Arcus.Messaging.Tests.Workers.ServiceBus;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Testing.Logging;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

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
        [InlineData(ServiceBusEntity.Topic, typeof(ServiceBusTopicProgram))]
        [InlineData(ServiceBusEntity.Topic, typeof(ServiceBusTopicContextPredicateSelectionProgram))]
        public async Task ServiceBusMessagePump_PublishServiceBusMessage_MessageSuccessfullyProcessed(ServiceBusEntity entity, Type programType)
        {
            // Arrange
            var config = TestConfig.Create();
            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", config.GetServiceBusConnectionString(entity)),
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync(programType, config, _logger, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(entity, config, _logger))
                {
                    // Act / Assert
                    await service.SimulateMessageProcessingAsync();
                }
            }
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithQueueAndTopic_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            const ServiceBusEntity entity = ServiceBusEntity.Queue;

            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING_WITH_QUEUE", config.GetServiceBusConnectionString(ServiceBusEntity.Queue)),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING_WITH_TOPIC", config.GetServiceBusConnectionString(ServiceBusEntity.Topic)),
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync<ServiceBusQueueAndTopicProgram>(config, _logger, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(entity, config, _logger))
                {
                    // Act / Assert
                    await service.SimulateMessageProcessingAsync();
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

            const ServiceBusEntity entity = ServiceBusEntity.Queue;
            var client = new ServiceBusClient(keyRotationConfig, _logger);

            const KeyType keyType = KeyType.SecondaryKey; 
            string freshConnectionString = await client.RotateConnectionStringKeysAsync(entity, keyType);

            ServicePrincipalAuthentication authentication = keyRotationConfig.ServicePrincipal.CreateAuthentication();
            IKeyVaultClient keyVaultClient = await authentication.AuthenticateAsync();
            await keyVaultClient.SetSecretAsync(
                vaultBaseUrl: keyRotationConfig.KeyVaultSecret.VaultUri,
                secretName: keyRotationConfig.KeyVaultSecret.SecretName,
                value: freshConnectionString);
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
                await using (var service = await TestMessagePumpService.StartNewAsync(entity, config, _logger))
                {
                    // Act
                    string rotatedConnectionString = await client.RotateConnectionStringKeysAsync(entity, keyType);
                    await keyVaultClient.SetSecretAsync(
                        vaultBaseUrl: keyRotationConfig.KeyVaultSecret.VaultUri,
                        secretName: keyRotationConfig.KeyVaultSecret.SecretName,
                        value: rotatedConnectionString);

                    // Assert
                    await service.SimulateMessageProcessingAsync();
                }
            }
        }
    }
}
