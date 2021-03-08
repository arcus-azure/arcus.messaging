using System;
using GuardNet;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents an endpoint where an Azure Key Vault event will be send to.
    /// </summary>
    public class KeyVaultEventEndpoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultEventEndpoint"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string to connect to the endpoint that will handle the Azure Key Vault event.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString"/> is blank.</exception>
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