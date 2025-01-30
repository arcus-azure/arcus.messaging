using System;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Pumps.EventHubs;
using Arcus.Messaging.Pumps.EventHubs.Configuration;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add a <see cref="AzureEventHubsMessagePump"/>
    /// and its <see cref="IAzureEventHubsMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a message pump to consume events from Azure EventHubs.
        /// </summary>
        /// <remarks>
        ///    Make sure that the application has the Arcus secret store configured correctly.
        ///    For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="eventHubsName">The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.</param>
        /// <param name="eventHubsConnectionStringSecretName">
        ///     The name of the secret to retrieve the Azure EventHubs connection string using your registered Arcus secret store (<see cref="ISecretProvider" />) implementation.
        /// </param>
        /// <param name="blobContainerName">
        ///     The name of the Azure Blob storage container in the storage account to reference where the event checkpoints will be stored and the load balanced.
        /// </param>
        /// <param name="storageAccountConnectionStringSecretName">
        ///     The name of the secret to retrieve the Azure EventHubs connection string using your registered Arcus secret store (<see cref="ISecretProvider" />) implementation.
        /// </param>
        /// <returns>A collection where the <see cref="IAzureEventHubsMessageHandler{TMessage}"/>s can be configured.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventHubsName"/>, the <paramref name="eventHubsConnectionStringSecretName"/>, the <paramref name="blobContainerName"/>,
        ///     or the <paramref name="storageAccountConnectionStringSecretName"/> is blank.
        /// </exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessagePump(
            this IServiceCollection services,
            string eventHubsName,
            string eventHubsConnectionStringSecretName,
            string blobContainerName,
            string storageAccountConnectionStringSecretName)
        {
            return AddEventHubsMessagePump(
                services,
                eventHubsName: eventHubsName,
                eventHubsConnectionStringSecretName: eventHubsConnectionStringSecretName,
                blobContainerName: blobContainerName,
                storageAccountConnectionStringSecretName: storageAccountConnectionStringSecretName,
                configureOptions: null);
        }

        /// <summary>
        /// Adds a message pump to consume events from Azure EventHubs.
        /// </summary>
        /// <remarks>
        ///    Make sure that the application has the Arcus secret store configured correctly.
        ///    For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="eventHubsName">The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.</param>
        /// <param name="eventHubsConnectionStringSecretName">
        ///     The name of the secret to retrieve the Azure EventHubs connection string using your registered Arcus secret store (<see cref="ISecretProvider" />) implementation.
        /// </param>
        /// <param name="blobContainerName">
        ///     The name of the Azure Blob storage container in the storage account to reference where the event checkpoints will be stored and the load balanced.
        /// </param>
        /// <param name="storageAccountConnectionStringSecretName">
        ///     The name of the secret to retrieve the Azure EventHubs connection string using your registered Arcus secret store (<see cref="ISecretProvider" />) implementation.
        /// </param>
        /// <param name="configureOptions">The function to configure additional options to influence the behavior of the message pump.</param>
        /// <returns>A collection where the <see cref="IAzureEventHubsMessageHandler{TMessage}"/>s can be configured.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventHubsName"/>, the <paramref name="eventHubsConnectionStringSecretName"/>, the <paramref name="blobContainerName"/>,
        ///     or the <paramref name="storageAccountConnectionStringSecretName"/> is blank.
        /// </exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessagePump(
            this IServiceCollection services,
            string eventHubsName,
            string eventHubsConnectionStringSecretName,
            string blobContainerName,
            string storageAccountConnectionStringSecretName,
            Action<AzureEventHubsMessagePumpOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(eventHubsName))
            {
                throw new ArgumentException("Requires a non-blank Azure Event hubs name to add a message pump", nameof(eventHubsName));
            }

            if (string.IsNullOrWhiteSpace(eventHubsConnectionStringSecretName))
            {
                throw new ArgumentException("Requires a non-blank secret name that points to an Azure Event Hubs connection string", nameof(eventHubsConnectionStringSecretName));
            }

            if (string.IsNullOrWhiteSpace(blobContainerName))
            {
                throw new ArgumentException("Requires a non-blank name for the Azure Blob container name, linked to the Azure Event Hubs", nameof(blobContainerName));
            }

            if (string.IsNullOrWhiteSpace(storageAccountConnectionStringSecretName))
            {
                throw new ArgumentException("Requires a non-blank secret name that points to an Azure Blob storage connection string", nameof(storageAccountConnectionStringSecretName));
            }

            return AddMessagePump(services, (serviceProvider, options) =>
            {
                ISecretProvider secretProvider = DetermineSecretProvider(serviceProvider);

                return AzureEventHubsMessagePumpConfig.CreateByConnectionString(
                    eventHubsName, eventHubsConnectionStringSecretName,
                    blobContainerName, storageAccountConnectionStringSecretName,
                    secretProvider, options);

            }, configureOptions);
        }

        /// <summary>
        /// Adds a message pump to consume events from Azure EventHubs.
        /// </summary>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="eventHubsName">The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.</param>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Event Hubs namespace to connect to.  This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="blobContainerUri">
        ///     The <see cref="Uri" /> referencing the blob container that includes the
        ///     name of the account and the name of the container.
        ///     This is likely to be similar to "https://{account_name}.blob.core.windows.net/{container_name}".
        /// </param>
        /// <returns>A collection where the <see cref="IAzureEventHubsMessageHandler{TMessage}"/>s can be configured.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventHubsName"/> or the <paramref name="fullyQualifiedNamespace"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="blobContainerUri"/> is not an absolute URI.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessagePumpUsingManagedIdentity(
            this IServiceCollection services,
            string eventHubsName,
            string fullyQualifiedNamespace,
            string blobContainerUri)
        {
            return AddEventHubsMessagePumpUsingManagedIdentity(
                services,
                eventHubsName: eventHubsName,
                fullyQualifiedNamespace: fullyQualifiedNamespace,
                blobContainerUri: blobContainerUri,
                configureOptions: null);
        }

        /// <summary>
        /// Adds a message pump to consume events from Azure EventHubs.
        /// </summary>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="eventHubsName">The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.</param>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Event Hubs namespace to connect to.  This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="blobContainerUri">
        ///     The <see cref="Uri" /> referencing the blob container that includes the
        ///     name of the account and the name of the container.
        ///     This is likely to be similar to "https://{account_name}.blob.core.windows.net/{container_name}".
        /// </param>
        /// <param name="configureOptions">The function to configure additional options to influence the behavior of the message pump.</param>
        /// <returns>A collection where the <see cref="IAzureEventHubsMessageHandler{TMessage}"/>s can be configured.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventHubsName"/> or the <paramref name="fullyQualifiedNamespace"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="blobContainerUri"/> is not an absolute URI.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessagePumpUsingManagedIdentity(
            this IServiceCollection services,
            string eventHubsName,
            string fullyQualifiedNamespace,
            string blobContainerUri,
            Action<AzureEventHubsMessagePumpOptions> configureOptions)
        {
            return AddEventHubsMessagePumpUsingManagedIdentity(
                services,
                eventHubsName: eventHubsName,
                fullyQualifiedNamespace: fullyQualifiedNamespace,
                blobContainerUri: blobContainerUri,
                clientId: null,
                configureOptions: configureOptions);
        }

        /// <summary>
        /// Adds a message pump to consume events from Azure EventHubs.
        /// </summary>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="eventHubsName">The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.</param>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Event Hubs namespace to connect to.  This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="blobContainerUri">
        ///     The <see cref="Uri" /> referencing the blob container that includes the
        ///     name of the account and the name of the container.
        ///     This is likely to be similar to "https://{account_name}.blob.core.windows.net/{container_name}".
        /// </param>
        /// <param name="clientId">
        ///     The client ID to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
        ///     <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm" />.
        /// </param>
        /// <returns>A collection where the <see cref="IAzureEventHubsMessageHandler{TMessage}"/>s can be configured.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventHubsName"/> or the <paramref name="fullyQualifiedNamespace"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="blobContainerUri"/> is not an absolute URI.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessagePumpUsingManagedIdentity(
            this IServiceCollection services,
            string eventHubsName,
            string fullyQualifiedNamespace,
            string blobContainerUri,
            string clientId)
        {
            return AddEventHubsMessagePumpUsingManagedIdentity(
                services,
                eventHubsName: eventHubsName,
                fullyQualifiedNamespace: fullyQualifiedNamespace,
                blobContainerUri: blobContainerUri,
                clientId: clientId,
                configureOptions: null);
        }

        /// <summary>
        /// Adds a message pump to consume events from Azure EventHubs.
        /// </summary>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="eventHubsName">The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.</param>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Event Hubs namespace to connect to.  This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="blobContainerUri">
        ///     The <see cref="Uri" /> referencing the blob container that includes the
        ///     name of the account and the name of the container.
        ///     This is likely to be similar to "https://{account_name}.blob.core.windows.net/{container_name}".
        /// </param>
        /// <param name="clientId">
        ///     The client ID to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
        ///     <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm" />.
        /// </param>
        /// <param name="configureOptions">The function to configure additional options to influence the behavior of the message pump.</param>
        /// <returns>A collection where the <see cref="IAzureEventHubsMessageHandler{TMessage}"/>s can be configured.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventHubsName"/> or the <paramref name="fullyQualifiedNamespace"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="blobContainerUri"/> is not an absolute URI.</exception>
        public static EventHubsMessageHandlerCollection AddEventHubsMessagePumpUsingManagedIdentity(
            this IServiceCollection services,
            string eventHubsName,
            string fullyQualifiedNamespace,
            string blobContainerUri,
            string clientId,
            Action<AzureEventHubsMessagePumpOptions> configureOptions)
        {
            if (string.IsNullOrWhiteSpace(eventHubsName))
            {
                throw new ArgumentException("Requires a non-blank Azure Event hubs name to add a message pump", nameof(eventHubsName));
            }

            if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            {
                throw new ArgumentException("Requires a non-blank Azure Event hubs fully-qualified namespace to add a message pump", nameof(eventHubsName));
            }

            if (string.IsNullOrWhiteSpace(blobContainerUri))
            {
                throw new ArgumentException("Requires a non-blank Azure Blob storage container endpoint to store event checkpoints and load balance the consumed event messages send to the message pump", nameof(blobContainerUri));
            }

            if (!Uri.IsWellFormedUriString(blobContainerUri, UriKind.Absolute))
            {
                throw new UriFormatException("Requires a valid absolute URI endpoint for the Azure Blob container to store event checkpoints and load balance the consumed event messages send to the message pump");
            }

            return AddMessagePump(services, (_, options) =>
            {
                return AzureEventHubsMessagePumpConfig.CreateByTokenCredential(
                    eventHubsName,
                    fullyQualifiedNamespace,
                    new Uri(blobContainerUri),
                    new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId }),
                    options);

            }, configureOptions);
        }

        private static EventHubsMessageHandlerCollection AddMessagePump(
            this IServiceCollection services,
            Func<IServiceProvider, AzureEventHubsMessagePumpOptions, AzureEventHubsMessagePumpConfig> createConfig,
            Action<AzureEventHubsMessagePumpOptions> configureOptions)
        {
            AzureEventHubsMessagePumpOptions options = CreateOptions(configureOptions);
            EventHubsMessageHandlerCollection collection = services.AddMessageRouter(options);

            services.AddMessagePump(serviceProvider =>
            {
                AzureEventHubsMessagePumpConfig config = createConfig(serviceProvider, options);
                return CreateMessagePump(serviceProvider, config);
            });

            return collection;
        }

        private static AzureEventHubsMessagePumpOptions CreateOptions(Action<AzureEventHubsMessagePumpOptions> configureOptions)
        {
            var options = new AzureEventHubsMessagePumpOptions();
            configureOptions?.Invoke(options);

            return options;
        }

        private static AzureEventHubsMessagePump CreateMessagePump(IServiceProvider serviceProvider, AzureEventHubsMessagePumpConfig eventHubsConfig)
        {
            var appConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
            var router = serviceProvider.GetService<IAzureEventHubsMessageRouter>();
            var logger = serviceProvider.GetRequiredService<ILogger<AzureEventHubsMessagePump>>();

            return new AzureEventHubsMessagePump(eventHubsConfig, appConfiguration, serviceProvider, router, logger);
        }

        private static EventHubsMessageHandlerCollection AddMessageRouter(this IServiceCollection services, AzureEventHubsMessagePumpOptions options)
        {
            EventHubsMessageHandlerCollection collection = services.AddEventHubsMessageRouting(provider =>
            {
                var logger = provider.GetService<ILogger<AzureEventHubsMessageRouter>>();
                return new AzureEventHubsMessageRouter(provider, options.Routing, logger);
            });
            collection.JobId = options.JobId;

            return collection;
        }

        private static ISecretProvider DetermineSecretProvider(IServiceProvider serviceProvider)
        {
            var secretProvider =
                serviceProvider.GetService<ICachedSecretProvider>()
                ?? serviceProvider.GetService<ISecretProvider>();

            if (secretProvider is null)
            {
                throw new InvalidOperationException(
                    "Could not retrieve the Azure EventHubs or Azure storage account connection string from the Arcus secret store because no secret store was configured in the application,"
                    + $"please configure the Arcus secret store with '{nameof(IHostBuilderExtensions.ConfigureSecretStore)}' on the application '{nameof(IHost)}' "
                    + $"or during the service collection registration 'AddSecretStore' on the application '{nameof(IServiceCollection)}'."
                    + "For more information on the Arcus secret store, see: https://security.arcus-azure.net/features/secret-store");
            }

            return secretProvider;
        }
    }
}
