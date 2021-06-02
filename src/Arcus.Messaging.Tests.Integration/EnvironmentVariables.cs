using Azure.Identity;

namespace Arcus.Messaging.Tests.Integration
{
    /// <summary>
    /// Represents the environment variable names that are required to authenticate to the Azure Service Bus with the <see cref="EnvironmentCredential"/>.
    /// </summary>
    public static class EnvironmentVariables
    {
        /// <summary>
        /// Gets the environment variable name that holds the Azure tenant ID.
        /// </summary>
        public const string AzureTenantId = "AZURE_TENANT_ID";

        /// <summary>
        /// Gets the environment variable name that holds the service principal client ID.
        /// </summary>
        public const string AzureServicePrincipalClientId = "AZURE_CLIENT_ID";

        /// <summary>
        /// Gets the environment variable name that holds the service principal client secret.
        /// </summary>
        public const string AzureServicePrincipalClientSecret = "AZURE_CLIENT_SECRET";
    }
}
