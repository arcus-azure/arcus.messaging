using Arcus.Testing;
using Azure.Core;
using Azure.Identity;

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
        public ServicePrincipal(string tenantId, string clientId, string clientSecret)
        {
            TenantId = tenantId;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public string TenantId { get; }

        /// <summary>
        /// Gets the ID of the client application.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the secret of the client application.
        /// </summary>
        public string ClientSecret { get; }

        /// <summary>
        /// Creates an instance that combines the service principal information into an <see cref="ClientCredential"/> instance.
        /// </summary>
        public TokenCredential GetCredential()
        {
            return new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        }
    }

    public static class ServicePrincipalConfigExtensions
    {
        /// <summary>
        /// Gets the service principal that can authenticate with the Azure resources used in these integration tests.
        /// </summary>
        /// <returns></returns>
        public static ServicePrincipal GetServicePrincipal(this TestConfig config)
        {
            var servicePrincipal = new ServicePrincipal(
                tenantId: config["Arcus:Infra:TenantId"],
                clientId: config["Arcus:Infra:ServicePrincipal:ClientId"],
                clientSecret: config["Arcus:Infra:ServicePrincipal:ClientSecret"]);

            return servicePrincipal;
        }
    }
}
