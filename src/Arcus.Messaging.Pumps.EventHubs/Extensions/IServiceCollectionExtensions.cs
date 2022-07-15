using System;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Pumps.EventHubs;
using Arcus.Messaging.Pumps.EventHubs.Configuration;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using GuardNet;
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
        /// Adds a message pump to consume messages from Azure EventHubs.
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
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the Azure EventHubs message pump");
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires a non-blank Azure EventHubs name where the events will be sent to when adding an Azure EvenHubs message pump");
            Guard.NotNullOrWhitespace(eventHubsConnectionStringSecretName, nameof(eventHubsConnectionStringSecretName), "Requires a non-blank secret name to retrieve the connection string to the Azure EventHubs where the message pump will retrieve its event messages");
            Guard.NotNullOrWhitespace(blobContainerName, nameof(blobContainerName), "Requires a non-blank Azure Blob storage container name to store event checkpoints and load balance the consumed event messages send to the message pump");
            Guard.NotNullOrWhitespace(storageAccountConnectionStringSecretName, nameof(storageAccountConnectionStringSecretName), "Requires a non-blank secret name to retrieve the connection string to the Azure Blob storage where the event checkpoints will be stored and events will be load balanced during the event processing of the message pump");

            return AddEventHubsMessagePump(
                services,
                eventHubsName,
                eventHubsConnectionStringSecretName,
                blobContainerName,
                storageAccountConnectionStringSecretName,
                configureOptions: null);
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure EventHubs.
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
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the Azure EventHubs message pump");
            Guard.NotNullOrWhitespace(eventHubsName, nameof(eventHubsName), "Requires a non-blank Azure EventHubs name where the events will be sent to when adding an Azure EvenHubs message pump");
            Guard.NotNullOrWhitespace(eventHubsConnectionStringSecretName, nameof(eventHubsConnectionStringSecretName), "Requires a non-blank secret name to retrieve the connection string to the Azure EventHubs where the message pump will retrieve its event messages");
            Guard.NotNullOrWhitespace(blobContainerName, nameof(blobContainerName), "Requires a non-blank Azure Blob storage container name to store event checkpoints and load balance the consumed event messages send to the message pump");
            Guard.NotNullOrWhitespace(storageAccountConnectionStringSecretName, nameof(storageAccountConnectionStringSecretName), "Requires a non-blank secret name to retrieve the connection string to the Azure Blob storage where the event checkpoints will be stored and events will be load balanced during the event processing of the message pump");
            
            EventHubsMessageHandlerCollection collection = services.AddEventHubsMessageRouting();
            services.AddHostedService(serviceProvider =>
            {
                var options = new AzureEventHubsMessagePumpOptions();
                configureOptions?.Invoke(options);

                ISecretProvider secretProvider = DetermineSecretProvider(serviceProvider);
                var eventHubsConfig = new AzureEventHubsMessagePumpConfig(
                    eventHubsName, eventHubsConnectionStringSecretName, blobContainerName, storageAccountConnectionStringSecretName, secretProvider, options);

                var appConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
                var router = serviceProvider.GetService<IAzureEventHubsMessageRouter>();
                var logger = serviceProvider.GetRequiredService<ILogger<AzureEventHubsMessagePump>>();

                return new AzureEventHubsMessagePump(eventHubsConfig, appConfiguration, serviceProvider, router, logger);
            });

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
