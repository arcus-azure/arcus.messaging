using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.ServiceBus;
using Arcus.Messaging.Tests.Workers.ServiceBus;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ServiceBus.Models;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Trait("Category", "Integration")]
    public class ServiceBusMessagePumpTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessagePumpTests"/> class.
        /// </summary>
        public ServiceBusMessagePumpTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
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
            string connectionString = config.GetServiceBusConnectionString(entity);
            var commandArguments = new[]
            {
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", config.GetTestInfraEventGridTopicUri()),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", config.GetTestInfraEventGridAuthKey()),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", connectionString),
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync(programType, config, _outputWriter, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _outputWriter))
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

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync<ServiceBusQueueAndTopicProgram>(config, _outputWriter, commandArguments))
            {
                await using (var service = await TestMessagePumpService.StartNewAsync(config, _outputWriter))
                {
                    // Act / Assert
                    await service.SimulateMessageProcessingAsync(connectionString);
                }
            }
        }

        [Fact]
        public async Task ServiceBusMessagePump_RotateServiceBusConnectionKeys_MessagePumpRestartsThenMessageSuccessfullyProcessed()
        {
            // Arrange
            var config = TestConfig.Create();
            KeyRotationConfig keyRotationConfig = config.GetKeyRotationConfig();
            _outputWriter.WriteLine("Using Service Principal [ClientID: '{0}']", keyRotationConfig.ServicePrincipal.ClientId);

            var client = new ServiceBusClient(keyRotationConfig, _outputWriter);
            string freshConnectionString = await client.RotateConnectionStringKeysAsync(KeyType.PrimaryKey);

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
            };

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync<ServiceBusQueueKeyVaultProgram>(config, _outputWriter, commandArguments))
            {
                string newSecondaryConnectionString = await client.RotateConnectionStringKeysAsync(KeyType.SecondaryKey);
                await SetConnectionStringInKeyVaultAsync(keyVaultClient, keyRotationConfig, newSecondaryConnectionString);

                await using (var service = await TestMessagePumpService.StartNewAsync(config, _outputWriter))
                {
                    // Act
                    string newPrimaryConnectionString = await client.RotateConnectionStringKeysAsync(KeyType.PrimaryKey);

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
            _outputWriter.WriteLine("Using Service Principal [ClientID: '{0}']", keyRotationConfig.ServicePrincipal.ClientId);

            var client = new ServiceBusClient(keyRotationConfig, _outputWriter);
            string freshConnectionString = await client.RotateConnectionStringKeysAsync(KeyType.PrimaryKey);

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

            using (var project = await ServiceBusWorkerProject.StartNewWithAsync<ServiceBusQueueSecretNewVersionReAuthenticateProgram>(config, _outputWriter, commandArguments))
            {
                string newSecondaryConnectionString = await client.RotateConnectionStringKeysAsync(KeyType.SecondaryKey);
                await SetConnectionStringInKeyVaultAsync(keyVaultClient, keyRotationConfig, newSecondaryConnectionString);

                await using (var service = await TestMessagePumpService.StartNewAsync(config, _outputWriter))
                {
                    // Act
                    string newPrimaryConnectionString = await client.RotateConnectionStringKeysAsync(KeyType.PrimaryKey);

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
    }
}
