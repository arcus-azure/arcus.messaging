using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace Arcus.Messaging.Pumps.ServiceBus
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
        /// <param name="clientId">The ID of the user or application that has the permissions to authenticate with and rotate connection strings on the Azure Service Bus.</param>
        /// <param name="clientSecretKey">
        ///     The secret key that points to the secret of the user or application that has the permissions to authenticate with and rotate connection strings on the Azure Service Bus.
        /// </param>
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
            Guard.NotNullOrWhitespace(clientId, nameof(clientId), "Requires an client ID with the necessary permissions to rotate Azure Service Bus connection string keys");
            Guard.NotNullOrWhitespace(clientSecretKey, nameof(clientSecretKey), "Requires an secret name that points to the client secret with the necessary permissions to rotate Azure Service Bus connection string keys");
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId), "Requires an account subscription ID that is in change of managing the Azure Service Bus resource");
            Guard.NotNullOrWhitespace(tenantId, nameof(tenantId), "Requires an tenant ID where the Azure Service Bus resource is located");
            Guard.NotNull(secretProvider, nameof(secretProvider), $"Requires an '{nameof(ISecretProvider)}' implementation to retrieve the client secret by requesting the '{nameof(clientSecretKey)}'");

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
        /// <exception cref="AuthenticationException">Thrown when the previously configured <see cref="ISecretProvider"/> isn't returning the client secret.</exception>
        public async Task<IServiceBusManagementClient> AuthenticateAsync()
        {
            Task<string> rawSecretAsync = _secretProvider.GetRawSecretAsync(_clientSecretKey);
            if (rawSecretAsync is null)
            {
                throw new AuthenticationException(
                    $"Could not authenticate with the Azure Service Bus because the '{nameof(ISecretProvider)}' that should have returned the client secret, returned 'null'");
            }

            string clientSecret = await rawSecretAsync;

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