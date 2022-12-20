using System;
using Arcus.Security.Core;
using Azure.Core.Extensions;
using Azure.Messaging.EventHubs.Producer;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Core;

// ReSharper disable once CheckNamespace
namespace  Microsoft.Extensions.Azure
{
    /// <summary>
    /// Extensions on the <see cref="IAzureClientFactoryBuilder"/> to add more easily Azure EventHubs producer clients with Arcus components.
    /// </summary>
    public static class AzureClientFactoryBuilderExtensions
    {
        /// <summary>
        /// Registers a <see cref="EventHubProducerClient" /> instance into the Azure client factory <paramref name="builder"/>
        /// via a connection string available via the <paramref name="connectionStringSecretName"/> in the Arcus secret store.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Make sure that the Arcus secret store is registered in the application before using this extension (<a href="https://security.arcus-azure.net/features/secret-store">more info</a>)
        ///     as the Azure EventHubs connection string will be retrieved via the <paramref name="connectionStringSecretName"/>.
        ///   </para>
        ///   <para>
        ///     If the connection string is copied from the Event Hubs namespace, it will likely not contain the name of the desired Event Hub, which is needed.
        ///     In this case, the name can be added manually by adding ";EntityPath=[[ EVENT HUB NAME ]]" to the end of the connection string. For example, ";EntityPath=telemetry-hub".
        ///     If you have defined a shared access policy directly on the Event Hub itself, then copying the connection string from that Event Hub will result in a connection string that contains the name.
        ///     <a href="https://docs.microsoft.com/azure/event-hubs/event-hubs-get-connection-string">How to get an Event Hubs connection string</a>
        ///   </para>
        /// </remarks>
        /// <param name="builder">The Azure client factory builder to add the Azure EventHubs producer client.</param>
        /// <param name="connectionStringSecretName">The secret name that corresponds with the Azure EventHubs connection string that is registered in the Arcus secret store.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionStringSecretName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the Arcus secret store is not registered.</exception>
        /// <exception cref="NotSupportedException">Thrown when the registered Arcus secret store does not have a synchronous secret provider registered.</exception>
        public static IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions> AddEventHubProducerClient(
            this AzureClientFactoryBuilder builder,
            string connectionStringSecretName)
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure client factory builder to add the Azure EventHubs producer client");
            Guard.NotNullOrWhitespace(connectionStringSecretName, nameof(connectionStringSecretName), "Requires a non-blank secret name to retrieve the Azure EventHubs connection string from the Arcus secret store");

            return AddEventHubProducerClient(builder, connectionStringSecretName: connectionStringSecretName, configureOptions: null);
        }

        /// <summary>
        /// Registers a <see cref="EventHubProducerClient" /> instance into the Azure client factory <paramref name="builder"/>
        /// via a connection string available via the <paramref name="connectionStringSecretName"/> in the Arcus secret store.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Make sure that the Arcus secret store is registered in the application before using this extension (<a href="https://security.arcus-azure.net/features/secret-store">more info</a>)
        ///     as the Azure EventHubs connection string will be retrieved via the <paramref name="connectionStringSecretName"/>.
        ///   </para>
        ///   <para>
        ///     If the connection string is copied from the Event Hubs namespace, it will likely not contain the name of the desired Event Hub, which is needed.
        ///     In this case, the name can be added manually by adding ";EntityPath=[[ EVENT HUB NAME ]]" to the end of the connection string. For example, ";EntityPath=telemetry-hub".
        ///     If you have defined a shared access policy directly on the Event Hub itself, then copying the connection string from that Event Hub will result in a connection string that contains the name.
        ///     <a href="https://docs.microsoft.com/azure/event-hubs/event-hubs-get-connection-string">How to get an Event Hubs connection string</a>
        ///   </para>
        /// </remarks>
        /// <param name="builder">The Azure client factory builder to add the Azure EventHubs producer client.</param>
        /// <param name="connectionStringSecretName">The secret name that corresponds with the Azure EventHubs connection string that is registered in the Arcus secret store.</param>
        /// <param name="configureOptions">The function to configure additional user option that alters the behavior of the Azure EventHubs interaction.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionStringSecretName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the Arcus secret store is not registered.</exception>
        /// <exception cref="NotSupportedException">Thrown when the registered Arcus secret store does not have a synchronous secret provider registered.</exception>
        public static IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions> AddEventHubProducerClient(
            this AzureClientFactoryBuilder builder,
            string connectionStringSecretName,
            Action<EventHubProducerClientOptions> configureOptions)
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure client factory builder to add the Azure EventHubs producer client");
            Guard.NotNullOrWhitespace(connectionStringSecretName, nameof(connectionStringSecretName), "Requires a non-blank secret name to retrieve the Azure EventHubs connection string from the Arcus secret store");

            return builder.AddClient<EventHubProducerClient, EventHubProducerClientOptions>((options, serviceProvider) =>
            {
                string connectionString = GetEventHubsConnectionString(connectionStringSecretName, serviceProvider);
                configureOptions?.Invoke(options);

                return new EventHubProducerClient(connectionString, options);
            });
        }

         /// <summary>
        /// Registers a <see cref="EventHubProducerClient" /> instance into the Azure client factory <paramref name="builder"/>
        /// via a connection string available via the <paramref name="connectionStringSecretName"/> in the Arcus secret store.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Make sure that the Arcus secret store is registered in the application before using this extension (<a href="https://security.arcus-azure.net/features/secret-store">more info</a>)
        ///     as the Azure EventHubs connection string will be retrieved via the <paramref name="connectionStringSecretName"/>.
        ///   </para>
        ///   <para>
        ///     If the connection string is copied from the Event Hub itself, it will contain the name of the desired Event Hub,
        ///     and can be used directly without passing the <paramref name="eventHubName" />.
        ///     The name of the Event Hub should be passed only once, either as part of the connection string or separately.
        ///     <a href="https://docs.microsoft.com/azure/event-hubs/event-hubs-get-connection-string">How to get an Event Hubs connection string</a>.
        ///   </para>
        /// </remarks>
        /// <param name="builder">The Azure client factory builder to add the Azure EventHubs producer client.</param>
        /// <param name="connectionStringSecretName">The secret name that corresponds with the Azure EventHubs connection string that is registered in the Arcus secret store.</param>
        /// <param name="eventHubName">The name of the specific Event Hub to associate the producer with.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionStringSecretName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the Arcus secret store is not registered.</exception>
        /// <exception cref="NotSupportedException">Thrown when the registered Arcus secret store does not have a synchronous secret provider registered.</exception>
        public static IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions> AddEventHubProducerClient(
            this AzureClientFactoryBuilder builder,
            string connectionStringSecretName,
            string eventHubName)
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure client factory builder to add the Azure EventHubs producer client");
            Guard.NotNullOrWhitespace(connectionStringSecretName, nameof(connectionStringSecretName), "Requires a non-blank secret name to retrieve the Azure EventHubs connection string from the Arcus secret store");
            Guard.NotNullOrWhitespace(eventHubName, nameof(eventHubName), "Requires a non-blank Azure EventHubs name to register the Azure EventHubs producer client");

            return AddEventHubProducerClient(builder, connectionStringSecretName: connectionStringSecretName, eventHubName, configureOptions: null);
        }

        /// <summary>
        /// Registers a <see cref="EventHubProducerClient" /> instance into the Azure client factory <paramref name="builder"/>
        /// via a connection string available via the <paramref name="connectionStringSecretName"/> in the Arcus secret store.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Make sure that the Arcus secret store is registered in the application before using this extension (<a href="https://security.arcus-azure.net/features/secret-store">more info</a>)
        ///     as the Azure EventHubs connection string will be retrieved via the <paramref name="connectionStringSecretName"/>.
        ///   </para>
        ///   <para>
        ///     If the connection string is copied from the Event Hub itself, it will contain the name of the desired Event Hub,
        ///     and can be used directly without passing the <paramref name="eventHubName" />.
        ///     The name of the Event Hub should be passed only once, either as part of the connection string or separately.
        ///     <a href="https://docs.microsoft.com/azure/event-hubs/event-hubs-get-connection-string">How to get an Event Hubs connection string</a>.
        ///   </para>
        /// </remarks>
        /// <param name="builder">The Azure client factory builder to add the Azure EventHubs producer client.</param>
        /// <param name="connectionStringSecretName">The secret name that corresponds with the Azure EventHubs connection string that is registered in the Arcus secret store.</param>
        /// <param name="eventHubName">The name of the specific Event Hub to associate the producer with.</param>
        /// <param name="configureOptions">The function to configure additional user option that alters the behavior of the Azure EventHubs interaction.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionStringSecretName"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the Arcus secret store is not registered.</exception>
        /// <exception cref="NotSupportedException">Thrown when the registered Arcus secret store does not have a synchronous secret provider registered.</exception>
        public static IAzureClientBuilder<EventHubProducerClient, EventHubProducerClientOptions> AddEventHubProducerClient(
            this AzureClientFactoryBuilder builder,
            string connectionStringSecretName,
            string eventHubName,
            Action<EventHubProducerClientOptions> configureOptions)
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure client factory builder to add the Azure EventHubs producer client");
            Guard.NotNullOrWhitespace(connectionStringSecretName, nameof(connectionStringSecretName), "Requires a non-blank secret name to retrieve the Azure EventHubs connection string from the Arcus secret store");
            Guard.NotNullOrWhitespace(eventHubName, nameof(eventHubName), "Requires a non-blank Azure EventHubs name to register the Azure EventHubs producer client");

            return builder.AddClient<EventHubProducerClient, EventHubProducerClientOptions>((options, serviceProvider) =>
            {
                string connectionString = GetEventHubsConnectionString(connectionStringSecretName, serviceProvider);
                configureOptions?.Invoke(options);

                return new EventHubProducerClient(connectionString, eventHubName, options);
            });
        }

        private static string GetEventHubsConnectionString(string connectionStringSecretName, IServiceProvider serviceProvider)
        {
            var secretProvider = serviceProvider.GetService<ISecretProvider>();
            if (secretProvider is null)
            {
                throw new InvalidOperationException(
                    "Requires an Arcus secret store registration to retrieve the connection string to authenticate with Azure EventHubs while creating an EventHubs producer client instance,"
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
                    serviceProvider.GetService<ILogger<EventHubProducerClient>>() 
                    ?? NullLogger<EventHubProducerClient>.Instance;

                logger.LogWarning(exception, "Cannot synchronously retrieve Azure EventHubs connection string secret for '{SecretName}', fallback on asynchronously", connectionStringSecretName);
                string connectionString = secretProvider.GetRawSecretAsync(connectionStringSecretName).GetAwaiter().GetResult();
                return connectionString;
            }
        }
    }
}
