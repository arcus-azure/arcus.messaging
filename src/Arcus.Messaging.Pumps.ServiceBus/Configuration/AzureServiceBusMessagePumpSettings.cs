using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store.</param>
        /// <param name="options">The options that influence the behavior of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <param name="serviceProvider">The collection of services to use during the lifetime of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="getConnectionStringFromConfigurationFunc"/> nor the <paramref name="getConnectionStringFromSecretFunc"/> is available.
        /// </exception>
        public AzureServiceBusMessagePumpSettings(
            string entityName,
            string subscriptionName,
            ServiceBusEntityType serviceBusEntity,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            AzureServiceBusMessagePumpOptions options, 
            IServiceProvider serviceProvider)
        {
            Guard.For<ArgumentException>(
                () => getConnectionStringFromConfigurationFunc is null && getConnectionStringFromSecretFunc is null,
                $"Requires an function that determines the connection string from either either an {nameof(IConfiguration)} or {nameof(ISecretProvider)} instance");
            Guard.NotNull(options, nameof(options), "Requires message pump options that influence the behavior of the message pump");
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a service provider to get additional registered services during the lifetime of the message pump");
            Guard.For<ArgumentException>(
                () => !Enum.IsDefined(typeof(ServiceBusEntityType), serviceBusEntity), 
                $"Azure Service Bus entity '{serviceBusEntity}' is not defined in the '{nameof(ServiceBusEntityType)}' enumeration");
            Guard.For<ArgumentOutOfRangeException>(
                () => serviceBusEntity is ServiceBusEntityType.Unknown, "Azure Service Bus entity type 'Unknown' is not supported here");
            
            _serviceProvider = serviceProvider;
            _getConnectionStringFromConfigurationFunc = getConnectionStringFromConfigurationFunc;
            _getConnectionStringFromSecretFunc = getConnectionStringFromSecretFunc;

            EntityName = entityName;
            SubscriptionName = subscriptionName;
            ServiceBusEntity = serviceBusEntity;
            Options = options;
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
            Guard.NotNull(options, nameof(options), "Requires message pump options that influence the behavior of the message pump");
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a service provider to get additional registered services during the lifetime of the message pump");
            Guard.NotNull(tokenCredential, nameof(tokenCredential), "Requires a token credential instance to authenticate with the Azure Service Bus");
            Guard.NotNullOrWhitespace(entityName, nameof(entityName), "Requires a non-blank entity name for the Azure Service Bus when using the token credentials");
            Guard.NotNullOrWhitespace(serviceBusNamespace, nameof(serviceBusNamespace), "Requires a non-blank fully qualified Azure Service Bus namespace when using the token credentials");
            Guard.For<ArgumentException>(() => !Enum.IsDefined(typeof(ServiceBusEntityType), serviceBusEntity), 
                $"Azure Service Bus entity '{serviceBusEntity}' is not defined in the '{nameof(ServiceBusEntityType)}' enumeration");
            Guard.For<ArgumentOutOfRangeException>(() => serviceBusEntity is ServiceBusEntityType.Unknown, 
                "Azure Service Bus entity type 'Unknown' is not supported here");
            
            _serviceProvider = serviceProvider;
            _tokenCredential = tokenCredential;

            EntityName = entityName;
            SubscriptionName = subscriptionName;
            ServiceBusEntity = serviceBusEntity;
            Options = options;
            
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

        /// <summary>
        /// Creates an <see cref="ServiceBusProcessor"/> instance based on the provided settings.
        /// </summary>
        internal async Task<ServiceBusProcessor> CreateMessageProcessorAsync()
        {
            var options = new ServiceBusClientOptions
            {
                RetryOptions = { TryTimeout = TimeSpan.FromSeconds(5) }
            };
            if (_tokenCredential is null)
            {
                string rawConnectionString = await GetConnectionStringAsync();
                string entityPath = DetermineEntityPath(rawConnectionString);
                
                var client = new ServiceBusClient(rawConnectionString, options);
                return CreateProcessor(client, entityPath, SubscriptionName);
            }
            else
            {
                var client = new ServiceBusClient(FullyQualifiedNamespace, _tokenCredential, options);

                string entityPath = DetermineEntityPath();
                ServiceBusProcessor processor = CreateProcessor(client, entityPath, SubscriptionName);

                return processor;
            }
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

        private ServiceBusProcessor CreateProcessor(ServiceBusClient client, string entityName, string subscriptionName)
        {
            ServiceBusProcessorOptions options = DetermineMessageProcessorOptions();
            
            if (string.IsNullOrWhiteSpace(subscriptionName))
            {
                return client.CreateProcessor(entityName, options);
            }

            return client.CreateProcessor(entityName, subscriptionName, options);
        }

        private ServiceBusProcessorOptions DetermineMessageProcessorOptions()
        {
            var messageHandlerOptions = new ServiceBusProcessorOptions();
            if (Options != null)
            {
                // Assign the configured defaults
                messageHandlerOptions.AutoCompleteMessages = Options.AutoComplete;
                messageHandlerOptions.MaxConcurrentCalls = Options.MaxConcurrentCalls ?? messageHandlerOptions.MaxConcurrentCalls;
            }

            return messageHandlerOptions;
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
