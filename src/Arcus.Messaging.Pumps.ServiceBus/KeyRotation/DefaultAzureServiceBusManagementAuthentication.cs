using System;
using System.Threading.Tasks;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace Arcus.Messaging.Pumps.ServiceBus.KeyRotation
{
    /// <summary>
    /// Represents the authentication with the Azure Service Bus.
    /// </summary>
    public class DefaultAzureServiceBusManagementAuthentication : IAzureServiceBusManagementAuthentication
    {
        private readonly string _clientId, _clientSecretKey, _subscriptionId, _tenantId;
        private readonly ISecretProvider _secretProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAzureServiceBusManagementAuthentication"/> class.
        /// </summary>
        /// <param name="clientId">The ID of the user or application that has the permissions to authenticate with the Azure Service Bus.</param>
        /// <param name="clientSecretKey">The secret key that points to the secret of the user or application that has the permissions to authenticate with the Azure Service Bus.</param>
        /// <param name="subscriptionId">The ID of the account subscription that manages the Azure Service Bus.</param>
        /// <param name="tenantId">The ID of the tenant where the Azure Service Bus is located.</param>
        /// <param name="secretProvider">The provider to retrieve the user or application secret with the specified <paramref name="clientSecretKey"/>.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="clientId"/>, <paramref name="clientSecretKey"/>, <paramref name="subscriptionId"/>, or <paramref name="tenantId"/> is blank.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> is <c>null</c>.</exception>
        public DefaultAzureServiceBusManagementAuthentication(
            string clientId,
            string clientSecretKey,
            string subscriptionId,
            string tenantId,
            ISecretProvider secretProvider)
        {
            Guard.NotNullOrWhitespace(clientId, nameof(clientId));
            Guard.NotNullOrWhitespace(clientSecretKey, nameof(clientSecretKey));
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId));
            Guard.NotNullOrWhitespace(tenantId, nameof(tenantId));
            Guard.NotNull(secretProvider, nameof(secretProvider));

            _secretProvider = secretProvider;
            _clientId = clientId;
            _clientSecretKey = clientSecretKey;
            _subscriptionId = subscriptionId;
            _tenantId = tenantId;
        }

        /// <summary>
        /// Authenticates with to the previously specified Azure Service Bus resource.
        /// </summary>
        /// <returns>
        ///     An <see cref="IServiceBusManagementClient"/> instance that manages the previously specified Azure Service Bus resource.
        /// </returns>
        public async Task<IServiceBusManagementClient> AuthenticateAsync()
        {
            string clientSecret = await _secretProvider.GetRawSecretAsync(_clientSecretKey);

            var context = new AuthenticationContext($"https://login.microsoftonline.com/{_tenantId}");
            var clientCredentials = new ClientCredential(_clientId, clientSecret);
            AuthenticationResult result =
                await context.AcquireTokenAsync(
                    "https://management.azure.com/",
                    clientCredentials);

            var tokenCredentials = new TokenCredentials(result.AccessToken);
            var client = new ServiceBusManagementClient(tokenCredentials) { SubscriptionId = _subscriptionId };
            return client;
        }
    }
}