using System;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus.KeyRotation;
using GuardNet;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents a client to interact with a Azure Service Bus.
    /// </summary>
    public class AzureServiceBusClient
    {
        private readonly IAzureServiceBusManagementAuthentication _authentication;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusClient"/> class.
        /// </summary>
        /// <param name="authentication">The instance to authenticate with the Azure Service Bus resource.</param>
        /// <param name="namespace">The instance that specifies the location where the Azure Service Bus resource is located.</param>
        /// <param name="logger">The instance to write diagnostic messages during interaction with the Azure Service Bus.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="authentication"/>, <paramref name="namespace"/>, or <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public AzureServiceBusClient(
            IAzureServiceBusManagementAuthentication authentication,
            AzureServiceBusNamespace @namespace,
            ILogger logger)
        {
            Guard.NotNull(authentication, nameof(authentication), "Requires an authentication implementation to connect to the Azure Service Bus resource");
            Guard.NotNull(@namespace, nameof(@namespace), "Requires an instance to locate teh Service Bus resource on Azure");
            Guard.NotNull(logger, nameof(logger), "Requires an logger instance to write diagnostic trace messages when interacting with the Azure Service Bus resource");

            _authentication = authentication;
            _logger = logger;

            Namespace = @namespace;
        }

        /// <summary>
        /// Gets the location information that specifies where the Azure Service Bus resource is located.
        /// </summary>
        public AzureServiceBusNamespace Namespace { get; }

        /// <summary>
        /// Rotate either the primary or secondary connection string key of the Azure Service Bus resource.
        /// </summary>
        /// <param name="keyType">The type of the key to rotate.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="keyType"/> is outside the bounds of the enumeration.</exception>
        public async Task<string> RotateConnectionStringKeyAsync(KeyType keyType)
        {
            Guard.For<ArgumentOutOfRangeException>(
                () => !Enum.IsDefined(typeof(KeyType), keyType), 
                $"Requires the KeyType value to be either '{nameof(KeyType.PrimaryKey)}' or '{nameof(KeyType.SecondaryKey)}'");

            try
            {
                using IServiceBusManagementClient client = await _authentication.AuthenticateAsync();
                
                _logger.LogTrace(
                    "Start rotating {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'...",
                    keyType, Namespace.Entity, Namespace.EntityName);

                AccessKeys keys = await RegenerateAzureServiceBusKeysAsync(Namespace.Entity, Namespace.EntityName, keyType, client);

                _logger.LogInformation(
                    "Rotated {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'",
                    keyType, Namespace.Entity, Namespace.EntityName);

                return keyType switch
                {
                    KeyType.PrimaryKey => keys.PrimaryConnectionString,
                    KeyType.SecondaryKey => keys.SecondaryConnectionString,
                    _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "Unknown key type")
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception, "Failed to rotate the {KeyType} connection string of the Azure Service Bus {EntityType} '{EntityName}'", keyType, Namespace.Entity, Namespace.EntityName);
                
                throw;
            }
        }

        private async Task<AccessKeys> RegenerateAzureServiceBusKeysAsync(
            ServiceBusEntity entity,
            string entityName,
            KeyType keyType,
            IServiceBusManagementClient client)
        {
            var parameters = new RegenerateAccessKeyParameters(keyType);
            return entity switch
            {
                ServiceBusEntity.Queue => await client.Queues.RegenerateKeysAsync(
                    Namespace.ResourceGroup, Namespace.Namespace, entityName, Namespace.AuthorizationRuleName, parameters),
                ServiceBusEntity.Topic => await client.Topics.RegenerateKeysAsync(
                    Namespace.ResourceGroup, Namespace.Namespace, entityName, Namespace.AuthorizationRuleName, parameters),
                _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown entity type")
            };
        }
    }
}
