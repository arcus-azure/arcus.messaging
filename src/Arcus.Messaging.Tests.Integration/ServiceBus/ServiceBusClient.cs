using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using GuardNet;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.ServiceBus
{
    /// <summary>
    /// 
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
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<string> RotateConnectionStringKeysAsync()
        {
            const KeyType keyType = KeyType.SecondaryKey;
            string queueName = _configuration.ServiceBusQueue.QueueName;

            try
            {
                string tenantId = _configuration.ServiceBusQueue.TenantId;
                var context = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");

                ClientCredential credentials = _configuration.ServicePrincipal.CreateCredentials();
                AuthenticationResult result =
                    await context.AcquireTokenAsync(
                        "https://management.azure.com/",
                        credentials);

                var creds = new TokenCredentials(result.AccessToken);
                string subscriptionId = _configuration.ServiceBusQueue.SubscriptionId;

                using (var client = new ServiceBusManagementClient(creds) { SubscriptionId = subscriptionId })
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
                    return keys.SecondaryConnectionString;
                }
            }
            catch (Exception exception)
            {
                _logger.WriteLine("Failed to rotate the {0} connection string of the Azure Service Bus Queue '{1}': {2}, {3}", keyType, queueName, exception.Message);
                throw;
            }
        }
    }
}
