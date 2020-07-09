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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.ServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public class ServiceBusClient
    {
        private readonly KeyRotationConfig _configuration;

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusClient"/> class.
        /// </summary>
        public ServiceBusClient(KeyRotationConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration));

            _configuration = configuration;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<string> RotateConnectionStringKeysAsync()
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
                AccessKeys keys = await client.Queues.RegenerateKeysAsync(
                    _configuration.ServiceBusQueue.ResourceGroup,
                    _configuration.ServiceBusQueue.Namespace,
                    _configuration.ServiceBusQueue.QueueName,
                    _configuration.ServiceBusQueue.AuthorizationRuleName,
                    new RegenerateAccessKeyParameters(KeyType.SecondaryKey));

                return keys.SecondaryConnectionString;
            }
        }
    }
}
