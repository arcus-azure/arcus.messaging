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
        private readonly TestConfig _configuration;

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusClient"/> class.
        /// </summary>
        public ServiceBusClient(TestConfig configuration)
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
            string tenantId = "7517bc42-bcf8-4916-a677-b5753051f846";
            var context = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");

            AuthenticationResult result =
                await context.AcquireTokenAsync(
                    "https://management.azure.com/",
                    new ClientCredential("88e84e7b-7f06-45e3-933f-4945270e0f60", "23.HBxx@Oft9e3XjWRczJCBDXTazz/c/"));

            var creds = new TokenCredentials(result.AccessToken);
            string subscriptionId = "c1537527-c126-428d-8f72-1ac9f2c63c1f";

            using (var client = new ServiceBusManagementClient(creds) { SubscriptionId = subscriptionId })
            {
                string resourceGroupName = "codit-arcus-messaging";
                string namespaceName = "arcus-messaging-dev-we-key-rotation-tests";
                string authorizationRuleName = "ManageSendListen";
                string queueName = "arcus-messaging-dev-we-key-rotation-tests";
                string keyType = "SecondaryKey";

                AccessKeys keys = await client.Queues.RegenerateKeysAsync(
                    resourceGroupName,
                    namespaceName,
                    queueName,
                    authorizationRuleName,
                    new RegenerateAccessKeyParameters(KeyType.SecondaryKey));

                return keys.SecondaryConnectionString;
            }
        }
    }
}
