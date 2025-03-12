using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents a client to interact with a Azure Service Bus.
    /// </summary>
    [Obsolete("Will be removed in v3.0, please use Microsoft's built-in Azure SDK clients to construct " + nameof(ServiceBusManagementClient) + " instances which can rotate Azure Service bus keys")]
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
            _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
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
            if (!Enum.IsDefined(typeof(KeyType), keyType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(keyType), $"Requires the KeyType value to be either '{nameof(KeyType.PrimaryKey)}' or '{nameof(KeyType.SecondaryKey)}'");
            }

            try
            {
                using IServiceBusManagementClient client = await _authentication.AuthenticateAsync();

                _logger.LogTrace(
                    "Start rotating {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'...",
                    keyType, Namespace.Entity, Namespace.EntityName);

                AccessKeys keys = await RegenerateAzureServiceBusKeysAsync(Namespace.Entity, Namespace.EntityName, keyType, client);

                _logger.LogWarning(
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
                _logger.LogError(exception, "Failed to rotate the {KeyType} connection string of the Azure Service Bus {EntityType} '{EntityName}'", keyType, Namespace.Entity, Namespace.EntityName);

                throw;
            }
        }

        private async Task<AccessKeys> RegenerateAzureServiceBusKeysAsync(
            ServiceBusEntityType entity,
            string entityName,
            KeyType keyType,
            IServiceBusManagementClient client)
        {
            var parameters = new RegenerateAccessKeyParameters(keyType);
            return entity switch
            {
                ServiceBusEntityType.Queue => await client.Queues.RegenerateKeysAsync(
                    Namespace.ResourceGroup, Namespace.Namespace, entityName, Namespace.AuthorizationRuleName, parameters),
                ServiceBusEntityType.Topic => await client.Topics.RegenerateKeysAsync(
                    Namespace.ResourceGroup, Namespace.Namespace, entityName, Namespace.AuthorizationRuleName, parameters),
                _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown entity type")
            };
        }
    }
}
