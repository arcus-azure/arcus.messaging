using System;
using System.Collections.Generic;
using System.Text;
using GuardNet;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class KeyVaultSecret
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public KeyVaultSecret(string vaultUri, string secretName)
        {
            Guard.NotNullOrWhitespace(vaultUri, nameof(vaultUri));
            Guard.NotNullOrWhitespace(secretName, nameof(secretName));

            VaultUri = vaultUri;
            SecretName = secretName;
        }

        public string VaultUri { get; }

        public string SecretName { get; }
    }
}
