using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CS0618

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// Represents the required settings to authenticate and start an <see cref="AzureServiceBusMessagePump"/>.
    /// </summary>
    internal class AzureServiceBusMessagePumpSettings
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly Func<IServiceProvider, Task<(ServiceBusClient client, string entityPath)>> _clientImplementationFactory;
        private readonly Func<IServiceProvider, Task<(ServiceBusAdministrationClient client, string entityPath)>> _adminClientImplementationFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpSettings"/> class.
        /// </summary>
        /// <param name="entityName">The name of the entity to process.</param>
        /// <param name="subscriptionName">The name of the subscription to process.</param>
        /// <param name="serviceBusEntity">The entity type of the Azure Service Bus.</param>
        /// <param name="clientImplementationFactory">The function to look up the connection string from the configuration.</param>
        /// <param name="clientAdminImplementationFactory">The function to look up the connection string from the configuration.</param>
        /// <param name="options">The options that influence the behavior of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <param name="serviceProvider">The collection of services to use during the lifetime of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <paramref name="clientImplementationFactory"/>, <paramref name="options"/> or <paramref name="serviceProvider"/> is <c>null</c>.
        /// </exception>
        internal AzureServiceBusMessagePumpSettings(
            string entityName,
            string subscriptionName,
            ServiceBusEntityType serviceBusEntity,
            Func<IServiceProvider, ServiceBusClient> clientImplementationFactory,
            Func<IServiceProvider, ServiceBusAdministrationClient> clientAdminImplementationFactory,
            AzureServiceBusMessagePumpOptions options,
            IServiceProvider serviceProvider)
        {
            if (serviceBusEntity is ServiceBusEntityType.Topic && string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus topic subscription name", nameof(subscriptionName));
            }

            if (clientImplementationFactory is null)
            {
                throw new ArgumentNullException(nameof(clientImplementationFactory));
            }

            if (!Enum.IsDefined(typeof(ServiceBusEntityType), serviceBusEntity) || serviceBusEntity is ServiceBusEntityType.Unknown)
            {
                throw new ArgumentException(
                    $"Azure Service Bus entity type should either be '{ServiceBusEntityType.Queue}' or '{ServiceBusEntityType.Topic}'", nameof(serviceBusEntity));
            }

            if (serviceBusEntity is ServiceBusEntityType.Topic
                && options.TopicSubscription is TopicSubscription.Automatic
                && clientAdminImplementationFactory is null)
            {
                throw new InvalidOperationException(
                    $"Cannot register the Azure Service bus topic message pump as the '{nameof(AzureServiceBusMessagePumpOptions.TopicSubscription)}={TopicSubscription.Automatic}', " +
                    $"while there is no registration of a {nameof(ServiceBusAdministrationClient)} function passed along with the 'AddServiceBusTopicMessagePumpUsingManagedIdentity'; " +
                    $"please use another overload to pass function so that the message pump can automatically manage topic subscriptions during its lifetime, " +
                    $"see the feature documentation for more information: https://messaging.arcus-azure.net/");
            }


            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _clientImplementationFactory = provider =>
            {
                ServiceBusClient client = clientImplementationFactory(provider);
                return Task.FromResult((client, entityName));
            };

            if (clientAdminImplementationFactory != null)
            {
                _adminClientImplementationFactory = provider =>
                {
                    ServiceBusAdministrationClient adminClient = clientAdminImplementationFactory(provider);
                    return Task.FromResult((adminClient, entityName));
                };
            }

            EntityName = entityName;
            SubscriptionName = SanitizeSubscriptionName(subscriptionName, serviceProvider);
            ServiceBusEntity = serviceBusEntity;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpSettings"/> class.
        /// </summary>
        /// <param name="entityName">The name of the entity to process.</param>
        /// <param name="subscriptionName">The name of the subscription to process.</param>
        /// <param name="serviceBusEntity">The entity type of the Azure Service Bus.</param>
        /// <param name="getConnectionStringFromConfigurationFunc">The function to look up the connection string from the configuration.</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store.</param>
        /// <param name="options">The options that influence the behavior of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <param name="serviceProvider">The collection of services to use during the lifetime of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="getConnectionStringFromConfigurationFunc"/> nor the <paramref name="getConnectionStringFromSecretFunc"/> is available.
        /// </exception>
        [Obsolete("Will be removed in v3.0, please use the other constructor without the " + nameof(ISecretProvider))]
        public AzureServiceBusMessagePumpSettings(
            string entityName,
            string subscriptionName,
            ServiceBusEntityType serviceBusEntity,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            AzureServiceBusMessagePumpOptions options,
            IServiceProvider serviceProvider)
        {
            if (serviceBusEntity is ServiceBusEntityType.Topic && string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus topic subscription name", nameof(subscriptionName));
            }

            if (getConnectionStringFromConfigurationFunc is null && getConnectionStringFromSecretFunc is null)
            {
                throw new ArgumentException(
                    $"Requires an function that determines the connection string from either either an {nameof(IConfiguration)} or {nameof(ISecretProvider)} instance");
            }

            if (!Enum.IsDefined(typeof(ServiceBusEntityType), serviceBusEntity) || serviceBusEntity is ServiceBusEntityType.Unknown)
            {
                throw new ArgumentException(
                    $"Azure Service Bus entity type should either be '{ServiceBusEntityType.Queue}' or '{ServiceBusEntityType.Topic}'", nameof(serviceBusEntity));
            }

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _clientImplementationFactory = async provider =>
            {
                string connectionString = await GetConnectionStringAsync(
                    provider,
                    getConnectionStringFromConfigurationFunc,
                    getConnectionStringFromSecretFunc,
                    options,
                    serviceBusEntity,
                    entityName);

                string entityPath = DetermineEntityName(connectionString, entityName);

                var client = new ServiceBusClient(connectionString);
                return (client, entityPath);
            };

            _adminClientImplementationFactory = async provider =>
            {
                string connectionString = await GetConnectionStringAsync(
                    provider,
                    getConnectionStringFromConfigurationFunc,
                    getConnectionStringFromSecretFunc,
                    options,
                    serviceBusEntity,
                    entityName);

                string entityPath = DetermineEntityName(connectionString, entityName);
                var adminClient = new ServiceBusAdministrationClient(connectionString);

                return (adminClient, entityPath);
            };

            EntityName = entityName;
            SubscriptionName = SanitizeSubscriptionName(subscriptionName, serviceProvider);
            ServiceBusEntity = serviceBusEntity;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private static string SanitizeSubscriptionName(string subscriptionName, IServiceProvider provider)
        {
            var logger =
                provider.GetService<ILogger<AzureServiceBusMessagePump>>()
                ?? NullLogger<AzureServiceBusMessagePump>.Instance;

            if (subscriptionName != null && subscriptionName.Length > 50)
            {
                logger.LogWarning("Azure Service Bus Topic subscription name was truncated to 50 characters");
                subscriptionName = subscriptionName.Substring(0, 50);
            }

            return subscriptionName;
        }

        /// <summary>
        /// Gets the name of the Azure Service Bus entity to process.
        /// </summary>
        /// <remarks>This is optional as the connection string can contain the entity name</remarks>
        public string EntityName { get; private set; }

        /// <summary>
        /// Gets the name of the Azure Service Bus Topic subscription.
        /// </summary>
        /// <remarks>This is only applicable when using Azure Service Bus Topics</remarks>
        public string SubscriptionName { get; }

        /// <summary>
        /// Gets the type of the Azure Service Bus entity.
        /// </summary>
        public ServiceBusEntityType ServiceBusEntity { get; }

        /// <summary>
        /// Gets the additional options that influence the behavior of the message pump.
        /// </summary>
        public AzureServiceBusMessagePumpOptions Options { get; }

        /// <summary>
        /// Gets the administration client that handles the management of the Azure Service Bus resource.
        /// </summary>
        internal async Task<ServiceBusAdministrationClient> GetServiceBusAdminClientAsync()
        {
            if (_adminClientImplementationFactory is null)
            {
                throw new InvalidOperationException(
                    $"Cannot start the Azure Service bus topic message pump as the '{nameof(AzureServiceBusMessagePumpOptions.TopicSubscription)}={TopicSubscription.Automatic}', " +
                    $"while there is no registration of a {nameof(ServiceBusAdministrationClient)} function passed along with the 'AddServiceBusTopicMessagePumpUsingManagedIdentity'; " +
                    $"please use another overload to pass function so that the message pump can automatically manage topic subscriptions during its lifetime, " +
                    $"see the feature documentation for more information: https://messaging.arcus-azure.net/");
            }

            (ServiceBusAdministrationClient client, string entityPath) = await _adminClientImplementationFactory(_serviceProvider);
            EntityName = entityPath;

            return client;
        }

        /// <summary>
        /// Creates an <see cref="ServiceBusReceiver"/> instance based on the provided settings.
        /// </summary>
        internal async Task<ServiceBusReceiver> CreateMessageReceiverAsync()
        {
            (ServiceBusClient client, string entityPath) = await _clientImplementationFactory(_serviceProvider);
            EntityName = entityPath;

            return string.IsNullOrWhiteSpace(SubscriptionName)
                ? client.CreateReceiver(EntityName, new ServiceBusReceiverOptions { PrefetchCount = Options.PrefetchCount })
                : client.CreateReceiver(EntityName, SubscriptionName, new ServiceBusReceiverOptions { PrefetchCount = Options.PrefetchCount });
        }

        private static async Task<string> GetConnectionStringAsync(
            IServiceProvider provider,
            Func<IConfiguration, string> getConnectionStringFromConfig,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretAsync,
            AzureServiceBusMessagePumpOptions options,
            ServiceBusEntityType entityType,
            string userSetEntityName)
        {
            if (options.EmitSecurityEvents)
            {
                ILogger logger =
                    provider.GetService<ILogger<AzureServiceBusMessagePumpSettings>>()
                    ?? NullLogger<AzureServiceBusMessagePumpSettings>.Instance;

                logger.LogSecurityEvent("Get Azure Service Bus connection string", new Dictionary<string, object>
                {
                    ["Service Bus entity"] = entityType,
                    ["Entity name"] = userSetEntityName,
                    ["Job ID"] = options.JobId
                });
            }

            if (getConnectionStringFromSecretAsync != null)
            {
                return await GetConnectionStringFromSecretAsync(provider, getConnectionStringFromSecretAsync);
            }

            return GetConnectionStringFromConfiguration(provider, getConnectionStringFromConfig);
        }

        private static string DetermineEntityName(string connectionString, string userSetEntityName)
        {
            var properties = ServiceBusConnectionStringProperties.Parse(connectionString);
            if (string.IsNullOrWhiteSpace(properties.EntityPath))
            {
                // Connection string doesn't include the entity so we're using the message pump settings
                if (string.IsNullOrWhiteSpace(userSetEntityName))
                {
                    throw new ArgumentException("No Azure Service Bus entity name was specified while the connection string is scoped to the namespace");
                }

                return userSetEntityName;
            }

            return properties.EntityPath;
        }

        private static string GetConnectionStringFromConfiguration(
            IServiceProvider provider,
            Func<IConfiguration, string> getConnectionStringFromConfig)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            return getConnectionStringFromConfig(configuration);
        }

        private static async Task<string> GetConnectionStringFromSecretAsync(
            IServiceProvider provider,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretAsync)
        {
            ISecretProvider userDefinedSecretProvider =
                provider.GetService<ICachedSecretProvider>()
                ?? provider.GetService<ISecretProvider>();

            if (userDefinedSecretProvider is null)
            {
                throw new InvalidOperationException(
                    "Could not retrieve the Azure Service Bus connection string from the Arcus secret store because no secret store was configured in the application,"
                    + $"please configure the Arcus secret store with '{nameof(IHostBuilderExtensions.ConfigureSecretStore)}' on the application '{nameof(IHost)}' "
                    + $"or during the service collection registration 'AddSecretStore' on the application '{nameof(IServiceCollection)}'."
                    + "For more information on the Arcus secret store, see: https://security.arcus-azure.net/features/secret-store");
            }

            Task<string> getConnectionStringTask = getConnectionStringFromSecretAsync(userDefinedSecretProvider);
            if (getConnectionStringTask is null)
            {
                throw new InvalidOperationException(
                    $"Cannot retrieve Azure Service Bus connection string via calling the {nameof(ISecretProvider)} because the operation resulted in 'null'");
            }

            return await getConnectionStringTask;
        }
    }
}
