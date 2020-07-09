using System;
using System.Collections.Generic;
using System.Text;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class KeyRotationConfig
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public KeyRotationConfig(KeyVaultSecret keyVaultSecret, ServicePrincipal servicePrincipal, ServiceBusQueue serviceBusQueue)
        {
            KeyVaultSecret = keyVaultSecret;
            ServicePrincipal = servicePrincipal;
            ServiceBusQueue = serviceBusQueue;
        }

        public KeyVaultSecret KeyVaultSecret { get; }

        public ServicePrincipal ServicePrincipal { get; }

        public ServiceBusQueue ServiceBusQueue { get; }
    }
}
