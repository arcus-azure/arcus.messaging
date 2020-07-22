using GuardNet;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a secret inside an Azure Key Vault instance.
    /// </summary>
    public class KeyVaultSecret
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultSecret" /> class.
        /// </summary>
        /// <param name="vaultUri">The URI referencing the Azure Key Vault instance.</param>
        /// <param name="secretName">The name of the secret in the Azure Key Vault instance.</param>
        public KeyVaultSecret(string vaultUri, string secretName)
        {
            Guard.NotNullOrWhitespace(vaultUri, nameof(vaultUri));
            Guard.NotNullOrWhitespace(secretName, nameof(secretName));

            VaultUri = vaultUri;
            SecretName = secretName;
        }

        /// <summary>
        /// Gets the URI referencing the Azure Key Vault instance.
        /// </summary>
        public string VaultUri { get; }

        /// <summary>
        /// Gets the name of the secret in the Azure Key Vault instance.
        /// </summary>
        public string SecretName { get; }
    }
}
