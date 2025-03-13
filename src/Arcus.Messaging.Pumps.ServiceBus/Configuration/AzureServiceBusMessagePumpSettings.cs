using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Azure.Core;
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
    public class AzureServiceBusMessagePumpSettings
    {
        private readonly Func<ISecretProvider, Task<string>> _getConnectionStringFromSecretFunc;
        private readonly Func<IConfiguration, string> _getConnectionStringFromConfigurationFunc;
        private readonly TokenCredential _tokenCredential;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpSettings"/> class.
        /// </summary>
        /// <param name="entityName">The name of the entity to process.</param>
        /// <param name="subscriptionName">The name of the subscription to process.</param>
        /// <param name="serviceBusEntity">The entity type of the Azure Service Bus.</param>
        /// <param name="getConnectionStringFromConfigurationFunc">The function to look up the connection string from the configuration.</param>
        /// <param name="options">The options that influence the behavior of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <param name="serviceProvider">The collection of services to use during the lifetime of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <paramref name="getConnectionStringFromConfigurationFunc"/>, <paramref name="options"/> or <paramref name="serviceProvider"/> is <c>null</c>.
        /// </exception>
        public AzureServiceBusMessagePumpSettings(
            string entityName,
            string subscriptionName,
            ServiceBusEntityType serviceBusEntity,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            AzureServiceBusMessagePumpOptions options,
            IServiceProvider serviceProvider)
        {
            if (serviceBusEntity is ServiceBusEntityType.Topic && string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus topic subscription name", nameof(subscriptionName));
            }

            if (getConnectionStringFromConfigurationFunc is null)
            {
                throw new ArgumentNullException(nameof(getConnectionStringFromConfigurationFunc));
            }

            if (!Enum.IsDefined(typeof(ServiceBusEntityType), serviceBusEntity) || serviceBusEntity is ServiceBusEntityType.Unknown)
            {
                throw new ArgumentException(
                    $"Azure Service Bus entity type should either be '{ServiceBusEntityType.Queue}' or '{ServiceBusEntityType.Topic}'", nameof(serviceBusEntity));
            }

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _getConnectionStringFromConfigurationFunc = getConnectionStringFromConfigurationFunc;

            EntityName = entityName;
            SubscriptionName = subscriptionName;
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
            _getConnectionStringFromConfigurationFunc = getConnectionStringFromConfigurationFunc;
            _getConnectionStringFromSecretFunc = getConnectionStringFromSecretFunc;

            EntityName = entityName;
            SubscriptionName = subscriptionName;
            ServiceBusEntity = serviceBusEntity;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessagePumpSettings"/> class.
        /// </summary>
        /// <param name="entityName">The name of the entity to process.</param>
        /// <param name="subscriptionName">The name of the subscription to process.</param>
        /// <param name="serviceBusEntity">The entity type of the Azure Service Bus.</param>
        /// <param name="serviceBusNamespace">
        ///     The Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="tokenCredential">The client credentials to authenticate with the Azure Service Bus.</param>
        /// <param name="options">The options that influence the behavior of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <param name="serviceProvider">The collection of services to use during the lifetime of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, <paramref name="serviceProvider"/>, or <paramref name="tokenCredential"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="serviceBusNamespace"/> is blank or the <paramref name="serviceBusEntity"/> is outside the bounds of the enumeration.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="serviceBusEntity"/> represents the unsupported value <see cref="ServiceBusEntityType.Unknown"/>.
        /// </exception>
        public AzureServiceBusMessagePumpSettings(
            string entityName,
            string subscriptionName,
            ServiceBusEntityType serviceBusEntity,
            string serviceBusNamespace,
            TokenCredential tokenCredential,
            AzureServiceBusMessagePumpOptions options,
            IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(entityName));
            }

            if (serviceBusEntity is ServiceBusEntityType.Topic && string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus topic subscription name", nameof(subscriptionName));
            }

            if (!Enum.IsDefined(typeof(ServiceBusEntityType), serviceBusEntity) || serviceBusEntity is ServiceBusEntityType.Unknown)
            {
                throw new ArgumentException(
                    $"Azure Service Bus entity type should either be '{ServiceBusEntityType.Queue}' or '{ServiceBusEntityType.Topic}'", nameof(serviceBusEntity));
            }

            if (string.IsNullOrWhiteSpace(serviceBusNamespace))
            {
                throw new ArgumentException("Requires a non-blank fully qualified Azure Service Bus namespace when using the token credentials", nameof(serviceBusNamespace));
            }

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _tokenCredential = tokenCredential ?? throw new ArgumentNullException(nameof(tokenCredential));

            EntityName = entityName;
            SubscriptionName = subscriptionName;
            ServiceBusEntity = serviceBusEntity;
            Options = options ?? throw new ArgumentNullException(nameof(options));

            if (serviceBusNamespace.EndsWith(".servicebus.windows.net"))
            {
                FullyQualifiedNamespace = serviceBusNamespace;
            }
            else
            {
                FullyQualifiedNamespace = serviceBusNamespace + ".servicebus.windows.net";
            }
        }

        /// <summary>
        /// Gets the name of the Azure Service Bus entity to process.
        /// </summary>
        /// <remarks>This is optional as the connection string can contain the entity name</remarks>
        public string EntityName { get; }

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
        /// Gets the fully qualified namespace where the Azure Service Bus entity is located.
        /// </summary>
        public string FullyQualifiedNamespace { get; }
        /// <summary>
        /// Gets the additional options that influence the behavior of the message pump.
        /// </summary>
        public AzureServiceBusMessagePumpOptions Options { get; internal set; }

        /// <summary>
        /// Gets the administration client that handles the management of the Azure Service Bus resource.
        /// </summary>
        internal async Task<ServiceBusAdministrationClient> GetServiceBusAdminClientAsync()
        {
            if (_tokenCredential is null)
            {
                string connectionString = await GetConnectionStringAsync();
                var client = new ServiceBusAdministrationClient(connectionString);

                return client;
            }
            else
            {
                var client = new ServiceBusAdministrationClient(FullyQualifiedNamespace, _tokenCredential);
                return client;
            }
        }

        /// <summary>
        /// Determines the path based on the provided settings where the Azure Service Bus entity is located.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when no entity path could be determined via the configured settings.</exception>
        public async Task<string> GetEntityPathAsync()
        {
            if (_tokenCredential is null)
            {
                string connectionString = await GetConnectionStringAsync();
                string entityPath = DetermineEntityPath(connectionString);

                return entityPath;
            }
            else
            {
                string entityPath = DetermineEntityPath();
                return entityPath;
            }
        }

        /// <summary>
        /// Creates an <see cref="ServiceBusReceiver"/> instance based on the provided settings.
        /// </summary>
        internal async Task<ServiceBusReceiver> CreateMessageReceiverAsync()
        {
            ServiceBusClient client;
            string entityPath;

            if (_tokenCredential is null)
            {
                string rawConnectionString = await GetConnectionStringAsync();
                entityPath = DetermineEntityPath(rawConnectionString);
                client = new ServiceBusClient(rawConnectionString);
            }
            else
            {
                client = new ServiceBusClient(FullyQualifiedNamespace, _tokenCredential);
                entityPath = DetermineEntityPath();
            }

            if (string.IsNullOrWhiteSpace(SubscriptionName))
            {
                return client.CreateReceiver(entityPath);
            }

            return client.CreateReceiver(entityPath, SubscriptionName);
        }

        private string DetermineEntityPath(string connectionString = null)
        {
            if (_tokenCredential is null && !string.IsNullOrWhiteSpace(connectionString))
            {
                var properties = ServiceBusConnectionStringProperties.Parse(connectionString);

                if (string.IsNullOrWhiteSpace(properties.EntityPath))
                {
                    // Connection string doesn't include the entity so we're using the message pump settings
                    if (string.IsNullOrWhiteSpace(EntityName))
                    {
                        throw new ArgumentException("No Azure Service Bus entity name was specified while the connection string is scoped to the namespace");
                    }

                    return EntityName;
                }

                return properties.EntityPath;
            }

            if (string.IsNullOrWhiteSpace(EntityName))
            {
                throw new ArgumentException("No Azure Service Bus entity name was specified while the managed identity authentication requires this");
            }

            return EntityName;
        }

        private async Task<string> GetConnectionStringAsync()
        {
            if (Options.EmitSecurityEvents)
            {
                ILogger logger =
                    _serviceProvider.GetService<ILogger<AzureServiceBusMessagePumpSettings>>()
                    ?? NullLogger<AzureServiceBusMessagePumpSettings>.Instance;

                logger.LogSecurityEvent("Get Azure Service Bus connection string", new Dictionary<string, object>
                {
                    ["Service Bus entity"] = ServiceBusEntity,
                    ["Entity name"] = EntityName,
                    ["Job ID"] = Options.JobId
                });
            }

            if (_getConnectionStringFromSecretFunc != null)
            {
                return await GetConnectionStringFromSecretAsync();
            }

            return GetConnectionStringFromConfiguration();
        }

        private string GetConnectionStringFromConfiguration()
        {
            var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
            return _getConnectionStringFromConfigurationFunc(configuration);
        }

        private async Task<string> GetConnectionStringFromSecretAsync()
        {
            ISecretProvider userDefinedSecretProvider =
                _serviceProvider.GetService<ICachedSecretProvider>()
                ?? _serviceProvider.GetService<ISecretProvider>();

            if (userDefinedSecretProvider is null)
            {
                throw new InvalidOperationException(
                    "Could not retrieve the Azure Service Bus connection string from the Arcus secret store because no secret store was configured in the application,"
                    + $"please configure the Arcus secret store with '{nameof(IHostBuilderExtensions.ConfigureSecretStore)}' on the application '{nameof(IHost)}' "
                    + $"or during the service collection registration 'AddSecretStore' on the application '{nameof(IServiceCollection)}'."
                    + "For more information on the Arcus secret store, see: https://security.arcus-azure.net/features/secret-store");
            }

            Task<string> getConnectionStringTask = _getConnectionStringFromSecretFunc(userDefinedSecretProvider);
            if (getConnectionStringTask is null)
            {
                throw new InvalidOperationException(
                    $"Cannot retrieve Azure Service Bus connection string via calling the {nameof(ISecretProvider)} because the operation resulted in 'null'");
            }

            return await getConnectionStringTask;
        }
    }
}
