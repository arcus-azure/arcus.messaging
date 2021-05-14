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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    ///     Settings for an Azure Service Bus message pump
    /// </summary>
    public class AzureServiceBusMessagePumpSettings
    {
        private readonly Func<ISecretProvider, Task<string>> _getConnectionStringFromSecretFunc;
        private readonly Func<IConfiguration, string> _getConnectionStringFromConfigurationFunc;
        private readonly TokenCredential _tokenCredential;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureServiceBusMessagePumpSettings"/> class.
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
        [Obsolete("Use the other constructor overload with the build-in '" + nameof(ServiceBusEntityType) + "' enumeration")]
        public AzureServiceBusMessagePumpSettings(
            string entityName,
            string subscriptionName,
            ServiceBusEntity serviceBusEntity,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            AzureServiceBusMessagePumpConfiguration options, 
            IServiceProvider serviceProvider)
            : this(entityName, subscriptionName, ConvertToServiceBusEntityType(serviceBusEntity), getConnectionStringFromConfigurationFunc, getConnectionStringFromSecretFunc, options, serviceProvider)
        {
        }

#pragma warning disable 618
        private static ServiceBusEntityType ConvertToServiceBusEntityType(ServiceBusEntity serviceBusEntity)
        {
            switch (serviceBusEntity)
            {
                case ServiceBus.ServiceBusEntity.Queue: return ServiceBusEntityType.Queue;
                case ServiceBus.ServiceBusEntity.Topic: return ServiceBusEntityType.Topic;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceBusEntity), serviceBusEntity, "Unknown Azure Service Bus entity");
            }
        }
#pragma warning restore 618
        
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
        ///     Thrown when the <paramref name="getConnectionStringFromConfigurationFunc"/> nor the <paramref name="getConnectionStringFromSecretFunc"/> is available;
        ///     or the <paramref name="serviceBusEntity"/> is outside the bounds of the enumeration.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="serviceBusEntity"/> represents the unsupported value <see cref="ServiceBusEntityType.Unknown"/>.
        /// </exception>
        public AzureServiceBusMessagePumpSettings(
            string entityName,
            string subscriptionName,
            ServiceBusEntityType serviceBusEntity,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            AzureServiceBusMessagePumpConfiguration options, 
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
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="tokenCredential">The client credentials to authenticate with the Azure Service Bus.</param>
        /// <param name="options">The options that influence the behavior of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <param name="serviceProvider">The collection of services to use during the lifetime of the <see cref="AzureServiceBusMessagePump"/>.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, <paramref name="serviceProvider"/>, or <paramref name="tokenCredential"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="fullyQualifiedNamespace"/> is blank or the <paramref name="serviceBusEntity"/> is outside the bounds of the enumeration.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="serviceBusEntity"/> represents the unsupported value <see cref="ServiceBusEntityType.Unknown"/>.
        /// </exception>
        public AzureServiceBusMessagePumpSettings(
            string entityName,
            string subscriptionName,
            ServiceBusEntityType serviceBusEntity,
            string fullyQualifiedNamespace,
            TokenCredential tokenCredential,
            AzureServiceBusMessagePumpConfiguration options,
            IServiceProvider serviceProvider)
        {
            Guard.NotNull(options, nameof(options), "Requires message pump options that influence the behavior of the message pump");
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a service provider to get additional registered services during the lifetime of the message pump");
            Guard.NotNull(tokenCredential, nameof(tokenCredential), "Requires a token credential instance to authenticate with the Azure Service Bus");
            Guard.NotNullOrWhitespace(entityName, nameof(entityName), "Requires a non-blank entity name for the Azure Service Bus when using the token credentials");
            Guard.NotNullOrWhitespace(fullyQualifiedNamespace, nameof(fullyQualifiedNamespace), "Requires a non-blank fully qualified Azure Service Bus namespace when using the token credentials");
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
            FullyQualifiedNamespace = fullyQualifiedNamespace;
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
        public AzureServiceBusMessagePumpConfiguration Options { get; internal set; }

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
        internal async Task<string> GetEntityPathAsync()
        {
            if (_tokenCredential is null)
            {
                string connectionString = await GetConnectionStringAsync();
                var properties = ServiceBusConnectionStringProperties.Parse(connectionString);

                return properties.EntityPath;
            }

            return EntityName;
        }
        
        /// <summary>
        /// Creates an <see cref="ServiceBusProcessor"/> instance based on the provided settings.
        /// </summary>
        internal async Task<ServiceBusProcessor> CreateMessageProcessorAsync()
        {
            if (_tokenCredential is null)
            {
                string rawConnectionString = await GetConnectionStringAsync();
                ServiceBusConnectionStringProperties serviceBusConnectionString = ServiceBusConnectionStringProperties.Parse(rawConnectionString);

                var client = new ServiceBusClient(rawConnectionString);
                {
                    ServiceBusProcessor processor;
                    if (string.IsNullOrWhiteSpace(serviceBusConnectionString.EntityPath))
                    {
                        // Connection string doesn't include the entity so we're using the message pump settings
                        if (string.IsNullOrWhiteSpace(EntityName))
                        {
                            throw new ArgumentException("No entity name was specified while the connection string is scoped to the namespace");
                        }

                        processor = CreateProcessor(client, EntityName, SubscriptionName);
                    }
                    else
                    {
                        // Connection string includes the entity so we're using that instead of the message pump settings
                        processor = CreateProcessor(client, serviceBusConnectionString.EntityPath, SubscriptionName);
                    }

                    return processor;
                }
            }
            else
            {
                var client = new ServiceBusClient(FullyQualifiedNamespace, _tokenCredential);
                ServiceBusProcessor processor = CreateProcessor(client, EntityName, SubscriptionName);

                return processor;
            }
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
                throw new KeyNotFoundException(
                    $"No configured {nameof(ICachedSecretProvider)} or {nameof(ISecretProvider)} implementation found in the service container. "
                    + "Please configure such an implementation (ex. in the Startup) of your application");
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
