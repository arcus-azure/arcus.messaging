using System;
using Arcus.Testing;
using Azure.Security.KeyVault.Secrets;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a secret inside an Azure Key Vault instance.
    /// </summary>
    public class KeyVaultConfig
    {
        private readonly ServicePrincipal _servicePrincipal;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultConfig" /> class.
        /// </summary>
        public KeyVaultConfig(ServicePrincipal servicePrincipal, string vaultName)
        {
            _servicePrincipal = servicePrincipal;
            VaultUri = $"https://{vaultName}.vault.azure.net";
        }

        /// <summary>
        /// Gets the URI referencing the Azure Key Vault instance.
        /// </summary>
        public string VaultUri { get; }

        public SecretClient GetClient()
        {
            return new SecretClient(new Uri(VaultUri), _servicePrincipal.GetCredential());
        }
    }

    public static class KeyVaultConfigExtensions
    {
        public static KeyVaultConfig GetKeyVault(this TestConfig config)
        {
            return new KeyVaultConfig(
                config.GetServicePrincipal(),
                config["Arcus:KeyVault:Name"]);
        }
    }
}
