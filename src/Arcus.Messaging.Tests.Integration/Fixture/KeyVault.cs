using GuardNet;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a secret inside an Azure Key Vault instance.
    /// </summary>
    public class KeyVault
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVault" /> class.
        /// </summary>
        /// <param name="vaultUri">The URI referencing the Azure Key Vault instance.</param>
        /// <param name="secretName">The name of the secret in the Azure Key Vault instance.</param>
        /// <param name="secretNewVersionCreated">The event endpoint of the Azure Key Vault 'Secret new version created' event.</param>
        public KeyVault(string vaultUri, string secretName, KeyVaultEventEndpoint secretNewVersionCreated)
        {
            Guard.NotNullOrWhitespace(vaultUri, nameof(vaultUri));
            Guard.NotNullOrWhitespace(secretName, nameof(secretName));
            Guard.NotNull(secretNewVersionCreated, nameof(secretNewVersionCreated));

            VaultUri = vaultUri;
            SecretName = secretName;
            SecretNewVersionCreated = secretNewVersionCreated;
        }

        /// <summary>
        /// Gets the URI referencing the Azure Key Vault instance.
        /// </summary>
        public string VaultUri { get; }

        /// <summary>
        /// Gets the name of the secret in the Azure Key Vault instance.
        /// </summary>
        public string SecretName { get; }

        /// <summary>
        /// Gets the event endpoint of the Azure Key Vault 'Secret new version created' event.
        /// </summary>
        public KeyVaultEventEndpoint SecretNewVersionCreated { get; }
    }

    public class KeyVaultEventEndpoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultEventEndpoint"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string to connect to the endpoint that will handle the Azure Key Vault event.</param>
        public KeyVaultEventEndpoint(string connectionString)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString));

            ConnectionString = connectionString;
        }

        /// <summary>
        /// Gets the connection string to connect to the endpoint that will handle the Azure Key Vault event.
        /// </summary>
        public string ConnectionString { get; }
    }
}
