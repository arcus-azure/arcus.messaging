using System;
using System.Collections.Generic;
using System.Text;
using Arcus.Security.Providers.AzureKeyVault.Authentication;
using GuardNet;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class ServicePrincipal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePrincipal"/> class.
        /// </summary>
        public ServicePrincipal(string clientId, string clientSecret)
        {
            Guard.NotNullOrWhitespace(clientId, nameof(clientId));
            Guard.NotNullOrWhitespace(clientSecret, nameof(clientSecret));

            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public string ClientId { get; }
        public string ClientSecret { get; }

        public ServicePrincipalAuthentication CreateAuthentication()
        {
            return new ServicePrincipalAuthentication(ClientId, ClientId);
        }

        public ClientCredential CreateCredentials()
        {
            return new ClientCredential(ClientId, ClientSecret);
        }
    }
}
