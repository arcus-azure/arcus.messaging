using System;
using Arcus.Testing;
using Azure.Core;
using Azure.Identity;
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
        public ServicePrincipal(string tenantId, string objectId, string clientId, string clientSecret)
        {
            TenantId = tenantId;
            ObjectId = Guid.Parse(objectId);
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public string TenantId { get; }

        /// <summary>
        /// Gets the ID of the client application.
        /// </summary>
        public string ClientId { get; }

        public Guid ObjectId { get; }

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
                tenantId: GetTenantId(config),
                objectId: config["Arcus:Infra:ServicePrincipal:ObjectId"],
                clientId: config["Arcus:Infra:ServicePrincipal:ClientId"],
                clientSecret: config["Arcus:Infra:ServicePrincipal:ClientSecret"]);

            return servicePrincipal;
        }

        /// <summary>
        /// Gets the ID of the current tenant where the Azure resources used in these integration tests are located.
        /// </summary>
        public static string GetTenantId(this TestConfig config)
        {
            return config["Arcus:Infra:TenantId"];
        }

        public static string GetSubscriptionId(this TestConfig config) => config["Arcus:Infra:SubscriptionId"];
        public static string GetResourceGroupName(this TestConfig config) => config["Arcus:Infra:ResourceGroup:Name"];
    }
}
