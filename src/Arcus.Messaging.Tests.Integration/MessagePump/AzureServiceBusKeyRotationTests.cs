using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.KeyRotation;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.ServiceBus;
using Arcus.Security.Core;
using Arcus.Security.Providers.AzureKeyVault;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Security.Providers.AzureKeyVault.Configuration;
using Arcus.Testing.Logging;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    [Trait(name: "Category", value: "Integration")]
    public class AzureServiceBusKeyRotationTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusKeyRotationTests"/> class.
        /// </summary>
        public AzureServiceBusKeyRotationTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task RotateServiceBusSecrets_WithValidArguments_RotatesPrimarySecondaryAlternatively()
        {
            // Arrange
            var config = TestConfig.Create();
            KeyRotationConfig keyRotationConfig = config.GetKeyRotationConfig();
            _logger.LogInformation("Using Service Principal [ClientID: '{ClientId}']", keyRotationConfig.ServicePrincipal.ClientId);
            const ServiceBusEntity entity = ServiceBusEntity.Topic;
            
            var keyVaultAuthentication = new ServicePrincipalAuthentication(
                keyRotationConfig.ServicePrincipal.ClientId,
                keyRotationConfig.ServicePrincipal.ClientSecret);

            var keyVaultConfiguration = new KeyVaultConfiguration(keyRotationConfig.KeyVault.VaultUri);
            var secretProvider = new KeyVaultSecretProvider(keyVaultAuthentication, keyVaultConfiguration);

            AzureServiceBusClient azureServiceBusClient = CreateAzureServiceBusClient(keyRotationConfig, secretProvider, entity);
            var rotation = new AzureServiceBusKeyRotation(azureServiceBusClient, keyVaultAuthentication, keyVaultConfiguration, _logger);

            var client = new ServiceBusConfiguration(keyRotationConfig, _logger);
            AccessKeys keysBefore1stRotation = await client.GetConnectionStringKeysForTopicAsync();

            // Act
            await rotation.RotateServiceBusSecretAsync(keyRotationConfig.KeyVault.SecretName);

            // Assert
            string secondaryConnectionString = await secretProvider.GetRawSecretAsync(keyRotationConfig.KeyVault.SecretName);
            AccessKeys keysAfter1stRotation = await client.GetConnectionStringKeysForTopicAsync();
            Assert.True(secondaryConnectionString == keysAfter1stRotation.SecondaryConnectionString, "Secondary connection string should be set in Azure Key Vault after first rotation");
            Assert.NotEqual(keysBefore1stRotation.PrimaryConnectionString, keysAfter1stRotation.PrimaryConnectionString);
            Assert.NotEqual(keysBefore1stRotation.SecondaryConnectionString, keysAfter1stRotation.SecondaryConnectionString);

            await rotation.RotateServiceBusSecretAsync(keyRotationConfig.KeyVault.SecretName);
            string primaryConnectionString = await secretProvider.GetRawSecretAsync(keyRotationConfig.KeyVault.SecretName);
            AccessKeys keysAfter2ndRotation = await client.GetConnectionStringKeysForTopicAsync();
            Assert.True(primaryConnectionString == keysAfter2ndRotation.PrimaryConnectionString, "Primary connection string should be set in Azure Key Vault after second rotation");
            Assert.NotEqual(keysAfter1stRotation.PrimaryConnectionString, keysAfter2ndRotation.PrimaryConnectionString);
            Assert.NotEqual(keysAfter2ndRotation.SecondaryConnectionString, keysAfter1stRotation.SecondaryConnectionString);
        }

        private AzureServiceBusClient CreateAzureServiceBusClient(
            KeyRotationConfig keyRotationConfig,
            ISecretProvider secretProvider,
            ServiceBusEntity entity)
        {
            var serviceBusAuthentication = new DefaultAzureServiceBusManagementAuthentication(
                keyRotationConfig.ServicePrincipal.ClientId,
                keyRotationConfig.ServicePrincipal.ClientSecretKey,
                keyRotationConfig.ServiceBusNamespace.SubscriptionId,
                keyRotationConfig.ServiceBusNamespace.TenantId,
                secretProvider);

            var serviceBusLocation = new AzureServiceBusNamespace(
                keyRotationConfig.ServiceBusNamespace.ResourceGroup,
                keyRotationConfig.ServiceBusNamespace.Namespace,
                entity,
                keyRotationConfig.ServiceBusNamespace.TopicName,
                keyRotationConfig.ServiceBusNamespace.AuthorizationRuleName);

            var azureServiceBusClient = new AzureServiceBusClient(serviceBusAuthentication, serviceBusLocation, _logger);
            return azureServiceBusClient;
        }
    }
}
