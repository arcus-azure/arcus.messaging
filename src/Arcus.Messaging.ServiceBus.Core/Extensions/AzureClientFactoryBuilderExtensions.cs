using System;
using Arcus.Security.Core;
using Azure.Core.Extensions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Azure
{
    /// <summary>
    /// Extensions on the <see cref="IAzureClientFactoryBuilder"/> to add more easily Azure Service Bus clients with Arcus components.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [Obsolete("Will be removed in v3.0, please use Microsoft's built-in Azure SDK clients to register a " + nameof(ServiceBusClient) + " to remove the " + nameof(ISecretProvider) + " requirement")]
    public static class AzureClientFactoryBuilderExtensions
    {
        /// <summary>
        /// Registers a <see cref="ServiceBusClient" /> instance into the Azure client factory <paramref name="builder"/>
        /// via a connection string available via the <paramref name="connectionStringSecretName"/> in the Arcus secret store.
        /// </summary>
        /// <remarks>
        ///     Make sure that the Arcus secret store is registered in the application before using this extension (<a href="https://security.arcus-azure.net/features/secret-store">more info</a>)
        ///     as the Azure Service Bus connection string will be retrieved via the <paramref name="connectionStringSecretName"/>.
        /// </remarks>
        /// <param name="builder">The Azure client factory builder to add the Azure Service Bus client.</param>
        /// <param name="connectionStringSecretName">The secret name that corresponds with the Azure Service Bus connection string that is registered in the Arcus secret store.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionStringSecretName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the Arcus secret store is not registered.</exception>
        /// <exception cref="SecretNotFoundException">Thrown when no Azure EventHubs connection string secret was found in the Arcus secret store.</exception>
        [Obsolete("Will be removed in v3.0, please use Microsoft's built-in Azure SDK clients to register a " + nameof(ServiceBusClient) + " to remove the " + nameof(ISecretProvider) + " requirement")]
        public static IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions> AddServiceBusClient(
            this AzureClientFactoryBuilder builder,
            string connectionStringSecretName)
        {
            return AddServiceBusClient(builder, connectionStringSecretName: connectionStringSecretName, configureOptions: null);
        }

        /// <summary>
        /// Registers a <see cref="ServiceBusClient" /> instance into the Azure client factory <paramref name="builder"/>
        /// via a connection string available via the <paramref name="connectionStringSecretName"/> in the Arcus secret store.
        /// </summary>
        /// <remarks>
        ///     Make sure that the Arcus secret store is registered in the application before using this extension (<a href="https://security.arcus-azure.net/features/secret-store">more info</a>)
        ///     as the Azure Service Bus connection string will be retrieved via the <paramref name="connectionStringSecretName"/>.
        /// </remarks>
        /// <param name="builder">The Azure client factory builder to add the Azure Service Bus client.</param>
        /// <param name="connectionStringSecretName">The secret name that corresponds with the Azure Service Bus connection string that is registered in the Arcus secret store.</param>
        /// <param name="configureOptions">The function to configure additional user option that alters the behavior of the Azure Service Bus interaction.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionStringSecretName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the Arcus secret store is not registered.</exception>
        /// <exception cref="SecretNotFoundException">Thrown when no Azure EventHubs connection string secret was found in the Arcus secret store.</exception>
        [Obsolete("Will be removed in v3.0, please use Microsoft's built-in Azure SDK clients to register a " + nameof(ServiceBusClient) + " to remove the " + nameof(ISecretProvider) + " requirement")]
        public static IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions> AddServiceBusClient(
            this AzureClientFactoryBuilder builder,
            string connectionStringSecretName,
            Action<ServiceBusClientOptions> configureOptions)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrWhiteSpace(connectionStringSecretName))
            {
                throw new ArgumentException("Requires a non-blank secret name to retrieve the Azure Service Bus connection string from the Arcus secret store", nameof(connectionStringSecretName));
            }

            return builder.AddClient<ServiceBusClient, ServiceBusClientOptions>((options, serviceProvider) =>
            {
                string connectionString = GetServiceBusConnectionString(connectionStringSecretName, serviceProvider);
                configureOptions?.Invoke(options);

                return new ServiceBusClient(connectionString, options);
            });
        }

        private static string GetServiceBusConnectionString(string connectionStringSecretName, IServiceProvider serviceProvider)
        {
            var secretProvider = serviceProvider.GetService<ISecretProvider>();
            if (secretProvider is null)
            {
                throw new InvalidOperationException(
                    "Requires an Arcus secret store registration to retrieve the connection string to authenticate with Azure Service Bus while creating an Service Bus client instance,"
                    + "please use the 'services.AddSecretStore(...)' or 'host.ConfigureSecretStore(...)' (https://security.arcus-azure.net/features/secret-store)");
            }

            try
            {
                string connectionString = secretProvider.GetRawSecret(connectionStringSecretName);
                return connectionString;
            }
            catch (Exception exception)
            {
                ILogger logger =
                    serviceProvider.GetService<ILogger<ServiceBusClient>>()
                    ?? NullLogger<ServiceBusClient>.Instance;

                logger.LogTrace(exception, "Cannot synchronously retrieve Azure Service Bus connection string secret for '{SecretName}', fallback on asynchronously", connectionStringSecretName);
                string connectionString = secretProvider.GetRawSecretAsync(connectionStringSecretName).GetAwaiter().GetResult();
                return connectionString;
            }
        }
    }
}
