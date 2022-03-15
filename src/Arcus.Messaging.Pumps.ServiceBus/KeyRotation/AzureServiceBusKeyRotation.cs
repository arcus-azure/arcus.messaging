using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using Arcus.Security.Providers.AzureKeyVault.Configuration;
using GuardNet;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus.KeyRotation
{
    /// <summary>
    /// Represents the functionality to rotate Azure Service Bus connection string keys stored inside a Azure Key Vault.
    /// </summary>
    [Obsolete("To auto-restart Azure Service Bus message pumps upon rotated credentials, please use the 'Arcus.BackgroundJobs.KeyVault' package instead")]
    public class AzureServiceBusKeyRotation
    {
        private readonly AzureServiceBusClient _serviceBusClient;
        private readonly IKeyVaultAuthentication _authentication;
        private readonly IKeyVaultConfiguration _configuration;
        private readonly ILogger _logger;

        private int _index = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusKeyRotation"/> class.
        /// </summary>
        /// <param name="serviceBusClient">The client to interact with the Azure Service Bus.</param>
        /// <param name="authentication">The instance to authenticate with the Azure Key Vault.</param>
        /// <param name="configuration">The instance containing the necessary configuration to interact with the Azure Key Vault.</param>
        /// <param name="logger">The instance to write diagnostic messages during rotation and interaction with the Azure Service Bus and Azure Key Vault.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="serviceBusClient"/>, <paramref name="authentication"/>, <paramref name="configuration"/>, or <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public AzureServiceBusKeyRotation(
            AzureServiceBusClient serviceBusClient,
            IKeyVaultAuthentication authentication,
            IKeyVaultConfiguration configuration,
            ILogger logger)
        {
            Guard.NotNull(serviceBusClient, nameof(serviceBusClient), "Requires an Azure Service Bus client to interact with the Service Bus when rotating the connection string keys");
            Guard.NotNull(authentication, nameof(authentication), "Requires an authentication instance to authenticate with the Azure Key Vault resource to set the new connection string keys");
            Guard.NotNull(configuration, nameof(configuration), "Requires an KeyVault configuration instance to locate the Key Vault resource on Azure");
            Guard.NotNull(logger, nameof(logger), "Requires an logger instance to write diagnostic trace messages when interacting with the Azure Service Bus and Azure Key Vault instances");

            _serviceBusClient = serviceBusClient;
            _authentication = authentication;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Rotates the Azure Service Bus connection string key, stored inside Azure Key Vault with the specified <paramref name="secretName"/>.
        /// </summary>
        /// <param name="secretName">The name of the secret where the Azure Service Bus connection string is stored.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        public async Task RotateServiceBusSecretAsync(string secretName)
        {
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), 
                "Requires a non-blank secret name that points to a secret in the Azure Key Vault resource to set the new rotated Azure Service Bus connection strings keys to");
            
            Interlocked.Increment(ref _index);

            using IKeyVaultClient keyVaultClient = await _authentication.AuthenticateAsync();

            if (_index % 2 != 0)
            {
                string secondaryConnectionString = await _serviceBusClient.RotateConnectionStringKeyAsync(KeyType.SecondaryKey);
                await SetConnectionStringSecretAsync(keyVaultClient, secretName, secondaryConnectionString, KeyType.SecondaryKey);

                await _serviceBusClient.RotateConnectionStringKeyAsync(KeyType.PrimaryKey);
            }
            else
            {
                string primaryConnectionString = await _serviceBusClient.RotateConnectionStringKeyAsync(KeyType.PrimaryKey);
                await SetConnectionStringSecretAsync(keyVaultClient, secretName, primaryConnectionString, KeyType.PrimaryKey);

                await _serviceBusClient.RotateConnectionStringKeyAsync(KeyType.SecondaryKey);
            }
        }

        private async Task SetConnectionStringSecretAsync(IKeyVaultClient keyVaultClient, string secretName, string connectionString, KeyType keyType)
        {
            string vaultUri = _configuration.VaultUri.OriginalString;
            ServiceBusEntityType entity = _serviceBusClient.Namespace.Entity;
            string entityName = _serviceBusClient.Namespace.EntityName;

            try
            {
                _logger.LogTrace("Setting {KeyType} Azure Service Bus {EntityType} '{EntityName}' connection string key into Azure Key Vault...", keyType, entity, entityName);
                await keyVaultClient.SetSecretAsync(vaultUri, secretName, connectionString);
                _logger.LogInformation("{KeyType} Azure Service Bus {EntityType} '{EntityName}' connection string key set into Azure Key Vault", keyType, entity, entityName);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception, "Unable to set the {KeyType} Azure Service Bus {EntityType} '{EntityName}' connection string key into Azure Key Vault", keyType, entity, entityName);

                throw;
            }
        }
    }
}