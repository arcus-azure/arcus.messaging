using System;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Integration.Fixture;
using GuardNet;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.ServiceBus
{
    /// <summary>
    /// Represents the test client to interact with a Azure Service Bus resource.
    /// </summary>
    public class ServiceBusConfiguration
    {
        private readonly KeyRotationConfig _configuration;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusConfiguration"/> class.
        /// </summary>
        /// <param name="configuration">The configuration instance to provide the necessary information during authentication with the correct Azure Service Bus instance.</param>
        /// <param name="logger">The instance to log diagnostic messages during the interaction with the Azure Service Bus instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> or the <paramref name="logger"/> is <c>null</c>.</exception>
        public ServiceBusConfiguration(KeyRotationConfig configuration, ILogger logger)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(logger, nameof(logger));

            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Gets the connection string keys for the Azure Service Bus Topic tested in the integration test suite.
        /// </summary>
        public async Task<AccessKeys> GetConnectionStringKeysForTopicAsync()
        {
            using IServiceBusManagementClient client = await CreateServiceManagementClientAsync();

            AccessKeys accessKeys = await client.Topics.ListKeysAsync(
                _configuration.ServiceBusNamespace.ResourceGroup,
                _configuration.ServiceBusNamespace.Namespace,
                _configuration.ServiceBusNamespace.TopicName,
                _configuration.ServiceBusNamespace.AuthorizationRuleName);

            return accessKeys;
        }

        /// <summary>
        /// Rotates the connection string key of the Azure Service Bus Queue, returning the new connection string as result.
        /// </summary>
        /// <param name="keyType">The type of key to rotate.</param>
        /// <returns>
        ///     The new connection string according to the <paramref name="keyType"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="keyType"/> is not within the bounds of the enumration.</exception>
        public async Task<string> RotateConnectionStringKeysForQueueAsync(KeyType keyType)
        {
            Guard.For<ArgumentOutOfRangeException>(
                () => !Enum.IsDefined(typeof(KeyType), keyType),
                $"Requires a KeyType that is either '{nameof(KeyType.PrimaryKey)}' or '{nameof(KeyType.SecondaryKey)}'");

            var parameters = new RegenerateAccessKeyParameters(keyType);
            string queueName = _configuration.ServiceBusNamespace.QueueName;
            const ServiceBusEntityType entity = ServiceBusEntityType.Queue;

            try
            {
                using IServiceBusManagementClient client = await CreateServiceManagementClientAsync();
                
                _logger.LogTrace(
                    "Start rotating {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'...",
                    keyType, entity, queueName);

                AccessKeys accessKeys = await client.Queues.RegenerateKeysAsync(
                    _configuration.ServiceBusNamespace.ResourceGroup,
                    _configuration.ServiceBusNamespace.Namespace,
                    queueName,
                    _configuration.ServiceBusNamespace.AuthorizationRuleName,
                    parameters);

                _logger.LogInformation(
                        "Rotated {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'",
                        keyType, entity, queueName);

                switch (keyType)
                {
                    case KeyType.PrimaryKey:   return accessKeys.PrimaryConnectionString;
                    case KeyType.SecondaryKey: return accessKeys.SecondaryConnectionString;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "Unknown key type");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception, "Failed to rotate the {KeyType} connection string of the Azure Service Bus {EntityType} '{EntityName}'", keyType, entity, queueName);
                
                throw;
            }
        }

        private async Task<IServiceBusManagementClient> CreateServiceManagementClientAsync()
        {
            string tenantId = _configuration.ServiceBusNamespace.TenantId;
            var context = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");

            AuthenticationResult result =
                await context.AcquireTokenAsync(
                    "https://management.azure.com/", 
                    new ClientCredential(
                        _configuration.ServicePrincipal.ClientId,
                        _configuration.ServicePrincipal.ClientSecret));

            var tokenCredentials = new TokenCredentials(result.AccessToken);
            string subscriptionId = _configuration.ServiceBusNamespace.SubscriptionId;

            var client = new ServiceBusManagementClient(tokenCredentials) { SubscriptionId = subscriptionId };
            return client;
        }
    }
}
