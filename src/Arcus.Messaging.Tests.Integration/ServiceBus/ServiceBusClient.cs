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
    public class ServiceBusClient
    {
        private readonly KeyRotationConfig _configuration;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusClient"/> class.
        /// </summary>
        public ServiceBusClient(KeyRotationConfig configuration, ILogger logger)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(logger, nameof(logger));

            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<AccessKeys> GetConnectionStringKeysAsync(ServiceBusEntity entity)
        {
            using IServiceBusManagementClient client = await CreateServiceManagementClientAsync();

            return entity switch
            {
                ServiceBusEntity.Queue => await client.Queues.ListKeysAsync(
                    _configuration.ServiceBusNamespace.ResourceGroup,
                    _configuration.ServiceBusNamespace.Namespace,
                    _configuration.ServiceBusNamespace.TopicName,
                    _configuration.ServiceBusNamespace.AuthorizationRuleName),
                ServiceBusEntity.Topic => await client.Topics.ListKeysAsync(
                    _configuration.ServiceBusNamespace.ResourceGroup,
                    _configuration.ServiceBusNamespace.Namespace,
                    _configuration.ServiceBusNamespace.TopicName,
                    _configuration.ServiceBusNamespace.AuthorizationRuleName),
                _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown key type")
            };
        }

        /// <summary>
        /// Rotates the connection string key of the Azure Service Bus Queue, returning the new connection string as result.
        /// </summary>
        /// <param name="entity">The type of the entity of the Azure Service Bus.</param>
        /// <param name="keyType">The type of key to rotate.</param>
        /// <returns>
        ///     The new connection string according to the <paramref name="keyType"/>.
        /// </returns>
        public async Task<string> RotateConnectionStringKeysAsync(ServiceBusEntity entity, KeyType keyType)
        {
            var parameters = new RegenerateAccessKeyParameters(keyType);
            string queueName = _configuration.ServiceBusNamespace.QueueName;

            try
            {
                using IServiceBusManagementClient client = await CreateServiceManagementClientAsync();
                
                _logger.LogTrace(
                    "Start rotating {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'...",
                    keyType, entity, queueName);
                
                AccessKeys keys = entity switch
                {
                    ServiceBusEntity.Topic =>
                        await client.Topics.RegenerateKeysAsync(
                            _configuration.ServiceBusNamespace.ResourceGroup,
                            _configuration.ServiceBusNamespace.Namespace,
                            queueName,
                            _configuration.ServiceBusNamespace.AuthorizationRuleName,
                            parameters),
                    ServiceBusEntity.Queue =>
                        await client.Queues.RegenerateKeysAsync(
                            _configuration.ServiceBusNamespace.ResourceGroup,
                            _configuration.ServiceBusNamespace.Namespace,
                            queueName,
                            _configuration.ServiceBusNamespace.AuthorizationRuleName,
                            parameters),
                    _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "Unknown key type")
                };

                _logger.LogInformation(
                    "Rotated {KeyType} connection string of Azure Service Bus {EntityType} '{EntityName}'",
                    keyType, entity, queueName);
                    
                return keys.SecondaryConnectionString;
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

            ClientCredential clientCredentials = _configuration.ServicePrincipal.CreateCredentials();
            AuthenticationResult result =
                await context.AcquireTokenAsync(
                    "https://management.azure.com/",
                    clientCredentials);

            var tokenCredentials = new TokenCredentials(result.AccessToken);
            string subscriptionId = _configuration.ServiceBusNamespace.SubscriptionId;

            var client = new ServiceBusManagementClient(tokenCredentials) { SubscriptionId = subscriptionId };
            return client;
        }
    }
}
