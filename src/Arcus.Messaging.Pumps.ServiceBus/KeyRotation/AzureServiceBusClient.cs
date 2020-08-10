using System;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace Arcus.Messaging.Pumps.ServiceBus.KeyRotation
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
        /// <param name="location">The instance that specifies the location where the Azure Service Bus resource is located.</param>
        /// <param name="logger">The instance to write diagnostic messages during interaction with the Azure Service Bus.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="authentication"/>, <paramref name="location"/>, or <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public AzureServiceBusClient(
            IAzureServiceBusManagementAuthentication authentication,
            AzureServiceBusLocation location,
            ILogger logger)
        {
            Guard.NotNull(authentication, nameof(authentication));
            Guard.NotNull(location, nameof(location));
            Guard.NotNull(logger, nameof(logger));

            _authentication = authentication;
            _logger = logger;

            Location = location;
        }

        /// <summary>
        /// Gets the location information that specifies where the Azure Service Bus resource is located.
        /// </summary>
        public AzureServiceBusLocation Location { get; }

        /// <summary>
        /// Rotate either the primary or secondary connection string key of the Azure Service Bus resource.
        /// </summary>
        /// <param name="keyType">The type of the key to rotate.</param>
        public async Task<string> RotateConnectionStringKeyAsync(KeyType keyType)
        {
            try
            {
                using IServiceBusManagementClient client = await _authentication.AuthenticateAsync();
                
                _logger.LogTrace(
                    "Start rotating {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'...",
                    keyType, Location.Entity, Location.EntityName);

                AccessKeys keys = await RegenerateAzureServiceBusKeysAsync(Location.Entity, Location.EntityName, keyType, client);

                _logger.LogInformation(
                    "Rotated {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'",
                    keyType, Location.Entity, Location.EntityName);

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
                    exception, "Failed to rotate the {KeyType} connection string of the Azure Service Bus {EntityType} '{EntityName}'", keyType, Location.Entity, Location.EntityName);
                
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
                    Location.ResourceGroup, Location.Namespace, entityName, Location.AuthorizationRuleName, parameters),
                ServiceBusEntity.Topic => await client.Topics.RegenerateKeysAsync(
                    Location.ResourceGroup, Location.Namespace, entityName, Location.AuthorizationRuleName, parameters),
                _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown entity type")
            };
        }
    }
}
