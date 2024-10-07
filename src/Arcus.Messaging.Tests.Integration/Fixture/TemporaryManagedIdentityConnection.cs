using System;
using Arcus.Testing;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a temporary connection to Azure with managed identity.
    /// </summary>
    internal class TemporaryManagedIdentityConnection : IDisposable
    {
        private readonly ILogger _logger;
        private readonly TemporaryEnvironmentVariable[] _environmentVariables;

        private TemporaryManagedIdentityConnection(
            string clientId,
            ILogger logger,
            params TemporaryEnvironmentVariable[] environmentVariables)
        {
            Guard.NotNull(environmentVariables, nameof(environmentVariables));
            Guard.NotAny(environmentVariables, nameof(environmentVariables));

            _logger = logger ?? NullLogger.Instance;
            _environmentVariables = environmentVariables;
            ClientId = clientId;
        }

        /// <summary>
        /// Gets the client ID of the user-assigned managed identity in this current connection.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Creates a <see cref="TemporaryManagedIdentityConnection"/> instance based on the current test <paramref name="configuration"/>.
        /// </summary>
        /// <param name="configuration">The current integration test configuration which includes the necessary settings to set up the managed identity connection.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the setup of the managed identity connection.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        internal static TemporaryManagedIdentityConnection Create(TestConfig configuration, ILogger logger)
        {
            Guard.NotNull(configuration, nameof(configuration));
            logger ??= NullLogger.Instance;

            string tenantId = configuration.GetTenantId();
            ServicePrincipal servicePrincipal = configuration.GetServicePrincipal();

            logger.LogTrace("Set managed identity connection for service principal: {ClientId}", servicePrincipal.ClientId);
            return new TemporaryManagedIdentityConnection(
                servicePrincipal.ClientId,
                logger,
                TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureTenantId, tenantId),
                TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientId, servicePrincipal.ClientId),
                TemporaryEnvironmentVariable.Create(EnvironmentVariables.AzureServicePrincipalClientSecret, servicePrincipal.ClientSecret));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _logger.LogTrace("Remove managed identity connection for service principal: {ClientId}", ClientId);
            Assert.All(_environmentVariables, envVar => envVar.Dispose());
        }
    }
}
