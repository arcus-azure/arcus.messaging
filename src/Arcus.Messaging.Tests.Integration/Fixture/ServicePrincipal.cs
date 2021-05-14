using Arcus.Security.Providers.AzureKeyVault.Authentication;
using GuardNet;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a registered service principal that can authenticate to Azure resources.
    /// </summary>
    public class ServicePrincipal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePrincipal"/> class.
        /// </summary>
        /// <param name="clientId">The ID of the client application.</param>
        /// <param name="clientSecret">The secret of the client application.</param>
        public ServicePrincipal(string clientId, string clientSecret) : this(clientId, clientSecret, clientSecret)
        {
            Guard.NotNullOrWhitespace(clientId, nameof(clientId));
            Guard.NotNullOrWhitespace(clientSecret, nameof(clientSecret));
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePrincipal"/> class.
        /// </summary>
        /// <param name="clientId">The ID of the client application.</param>
        /// <param name="clientSecret">The secret of the client application.</param>
        /// <param name="clientSecretKey">The key to the secret of the client application.</param>
        public ServicePrincipal(string clientId, string clientSecret, string clientSecretKey)
        {
            Guard.NotNullOrWhitespace(clientId, nameof(clientId));
            Guard.NotNullOrWhitespace(clientSecret, nameof(clientSecret));

            ClientId = clientId;
            ClientSecret = clientSecret;
            ClientSecretKey = clientSecretKey;
        }

        /// <summary>
        /// Gets the ID of the client application.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the secret of the client application.
        /// </summary>
        public string ClientSecret { get; }

        /// <summary>
        /// Gets the key to the client secret of the client application.
        /// </summary>
        public string ClientSecretKey { get; }

        /// <summary>
        /// Creates an instance to authenticate to Azure Key Vault.
        /// </summary>
        public ServicePrincipalAuthentication CreateAuthentication()
        {
            return new ServicePrincipalAuthentication(ClientId, ClientSecret);
        }

        /// <summary>
        /// Creates an instance that combines the service principal information into an <see cref="ClientCredential"/> instance.
        /// </summary>
        public ClientCredential CreateCredentials()
        {
            return new ClientCredential(ClientId, ClientSecret);
        }
    }
}
