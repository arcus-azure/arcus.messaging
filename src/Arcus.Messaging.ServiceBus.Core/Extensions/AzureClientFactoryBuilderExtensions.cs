using System;
using Arcus.Security.Core;
using Azure.Core.Extensions;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Azure
{
    /// <summary>
    /// Extensions on the <see cref="IAzureClientFactoryBuilder"/> to add more easily Azure Service Bus clients with Arcus components.
    /// </summary>
    // ReSharper disable once InconsistentNaming
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
        public static IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions> AddServiceBusClient(
            this AzureClientFactoryBuilder builder,
            string connectionStringSecretName)
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure client factory builder to add the Azure Service Bus client");
            Guard.NotNullOrWhitespace(connectionStringSecretName, nameof(connectionStringSecretName), "Requires a non-blank secret name to retrieve the Azure Service Bus connection string from the Arcus secret store");

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
        public static IAzureClientBuilder<ServiceBusClient, ServiceBusClientOptions> AddServiceBusClient(
            this AzureClientFactoryBuilder builder,
            string connectionStringSecretName,
            Action<ServiceBusClientOptions> configureOptions)
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure client factory builder to add the Azure Service Bus client");
            Guard.NotNullOrWhitespace(connectionStringSecretName, nameof(connectionStringSecretName), "Requires a non-blank secret name to retrieve the Azure Service Bus connection string from the Arcus secret store");
            
            return builder.AddClient<ServiceBusClient, ServiceBusClientOptions>((options, serviceProvider) =>
            {
                var secretProvider = serviceProvider.GetService<ISecretProvider>();
                if (secretProvider is null)
                {
                    throw new InvalidOperationException(
                        "Requires an Arcus secret store registration to retrieve the connection string to authenticate with Azure Service Bus while creating an Service Bus client instance," 
                        + "please use the 'services.AddSecretStore(...)' or 'host.ConfigureSecretStore(...)' (https://security.arcus-azure.net/features/secret-store)");
                }

                string connectionString = secretProvider.GetRawSecretAsync(connectionStringSecretName).GetAwaiter().GetResult();
                configureOptions?.Invoke(options);

                return new ServiceBusClient(connectionString, options);
            });
        }
    }
}
