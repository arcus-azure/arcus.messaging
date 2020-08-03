using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using GuardNet;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
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
        private readonly ITestOutputHelper _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusClient"/> class.
        /// </summary>
        public ServiceBusClient(KeyRotationConfig configuration, ITestOutputHelper logger)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(logger, nameof(logger));

            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Rotates the connection string key of the Azure Service Bus Queue, returning the new connection string as result.
        /// </summary>
        /// <param name="keyType">The type of key to rotate.</param>
        /// <returns>
        ///     The new connection string according to the <paramref name="keyType"/>.
        /// </returns>
        public async Task<string> RotateConnectionStringKeysAsync(KeyType keyType)
        {
            string queueName = _configuration.ServiceBusQueue.QueueName;

            try
            {
                string tenantId = _configuration.ServiceBusQueue.TenantId;
                var context = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");

                ClientCredential clientCredentials = _configuration.ServicePrincipal.CreateCredentials();
                AuthenticationResult result =
                    await context.AcquireTokenAsync(
                        "https://management.azure.com/",
                        clientCredentials);

                var tokenCredentials = new TokenCredentials(result.AccessToken);
                string subscriptionId = _configuration.ServiceBusQueue.SubscriptionId;

                using (var client = new ServiceBusManagementClient(tokenCredentials) { SubscriptionId = subscriptionId })
                {
                    _logger.WriteLine(
                        "Start rotating {0} connection string of Azure Service Bus Queue '{1}'...",
                        keyType, queueName);

                    AccessKeys keys = await client.Queues.RegenerateKeysAsync(
                        _configuration.ServiceBusQueue.ResourceGroup,
                        _configuration.ServiceBusQueue.Namespace,
                        queueName,
                        _configuration.ServiceBusQueue.AuthorizationRuleName,
                        new RegenerateAccessKeyParameters(keyType));

                    _logger.WriteLine(
                        "Rotated {0} connection string of Azure Service Bus Queue '{1}'",
                        keyType, queueName);

                    switch (keyType)
                    {
                        case KeyType.PrimaryKey: return keys.PrimaryConnectionString;
                        case KeyType.SecondaryKey: return keys.SecondaryConnectionString;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "Unknown key type");
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.WriteLine("Failed to rotate the {0} connection string of the Azure Service Bus Queue '{1}': {2}", keyType, queueName, exception.Message);
                throw;
            }
        }
    }
}
