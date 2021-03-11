using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// Represents the settings for an <see cref="AzureServiceBusMessagePump"/>.
    /// </summary>
    public class AzureServiceBusMessagePumpSettings
    {
        private readonly Func<ISecretProvider, Task<string>> _getConnectionStringFromSecretFunc;
        private readonly Func<IConfiguration, string> _getConnectionStringFromConfigurationFunc;
        private readonly ITokenProvider _tokenProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

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
            ServiceBusEntity serviceBusEntity,
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

            _serviceProvider = serviceProvider;
            _getConnectionStringFromConfigurationFunc = getConnectionStringFromConfigurationFunc;
            _getConnectionStringFromSecretFunc = getConnectionStringFromSecretFunc;
            _logger = (ILogger) _serviceProvider.GetService<ILogger<AzureServiceBusMessagePumpSettings>>() ?? NullLogger.Instance;
            
            EntityName = entityName;
            SubscriptionName = subscriptionName;
            ServiceBusEntity = serviceBusEntity;
            Options = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureActiveDirectoryTokenProvider"/> class.
        /// </summary>
        /// <param name="entityName">The name of the entity to process.</param>
        /// <param name="subscriptionName">The name of the subscription to process.</param>
        /// <param name="endpoint">The fully qualified domain name for your Azure Service Bus. Most likely, {yournamespace}.servicebus.windows.net.</param>
        /// <param name="serviceBusEntity">The entity type of the Service Bus.</param>
        /// <param name="options">The options that influence the behavior of the message pump.</param>
        /// <param name="tokenProvider">The instance that provides the security token to connect to the Azure Service Bus resource.</param>
        /// <param name="serviceProvider">The collection of services to use.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="entityName"/> or <paramref name="endpoint"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="tokenProvider"/>, <paramref name="options"/>, or <paramref name="serviceProvider"/> is <c>null</c>.
        /// </exception>
        public AzureServiceBusMessagePumpSettings(
            string entityName,
            string subscriptionName,
            string endpoint,
            ServiceBusEntity serviceBusEntity,
            AzureServiceBusMessagePumpConfiguration options,
            ITokenProvider tokenProvider,
            IServiceProvider serviceProvider)
        {
            Guard.NotNullOrWhitespace(entityName, nameof(entityName), "Requires a non-blank name of the Azure Service Bus Queue or Topic entity");
            Guard.NotNullOrWhitespace(endpoint, nameof(endpoint), "Requires a non-blank endpoint where the Azure Service Bus resource is located, most likely in the form: '{yournamespace}.servicebus.windows.net'");
            Guard.NotNull(options, nameof(options), "Requires a set of options to influence the behavior of the message pump");
            Guard.NotNull(tokenProvider, nameof(tokenProvider), "Requires an instance to provide security tokens to connect to the Azure Service Bus resource");
            Guard.NotNull(serviceProvider, nameof(serviceProvider), "Requires a collection of registered services that the message pump and message handling requires");

            _serviceProvider = serviceProvider;
            _tokenProvider = tokenProvider;
            _logger = (ILogger) _serviceProvider.GetService<ILogger<AzureServiceBusMessagePumpSettings>>() ?? NullLogger.Instance;
            
            EntityName = entityName;
            SubscriptionName = subscriptionName;
            Endpoint = endpoint;
            ServiceBusEntity = serviceBusEntity;
            Options = options;
        }

        /// <summary>
        /// Gets the name of the Queue or Topic entity to process.
        /// </summary>
        /// <remarks>This is optional as the connection string can contain the entity name</remarks>
        public string EntityName { get; }

        /// <summary>
        /// Gets the name of the Topic subscription to process.
        /// </summary>
        /// <remarks>This is only applicable when using Azure Service Bus Topics</remarks>
        public string SubscriptionName { get; }

        /// <summary>
        /// Gets the endpoint of the Azure Service Bus.
        /// </summary>
        public string Endpoint { get; }
        
        /// <summary>
        ///     Entity of the Service Bus.
        /// </summary>
        public ServiceBusEntity ServiceBusEntity { get; }

        /// <summary>
        ///     Options that influence the behavior of the message pump
        /// </summary>
        public AzureServiceBusMessagePumpConfiguration Options { get; internal set; }

        /// <summary>
        /// Creates an authenticated <see cref="MessageReceiver"/> instance to receive messages from the Azure Service Bus.
        /// </summary>
        internal async Task<MessageReceiver> CreateMessageReceiverAsync()
        {
            if (_tokenProvider is null)
            {
                string rawConnectionString = await GetConnectionStringAsync();
                var serviceBusConnectionStringBuilder = new ServiceBusConnectionStringBuilder(rawConnectionString);

                if (string.IsNullOrWhiteSpace(serviceBusConnectionStringBuilder.EntityPath))
                {
                    // Connection string doesn't include the entity so we're using the message pump settings
                    if (string.IsNullOrWhiteSpace(EntityName))
                    {
                        throw new ArgumentException("No entity name was specified while the connection string is scoped to the namespace");
                    }

                    string entityPath = GetEntityPath();
                    MessageReceiver messageReceiver = CreateReceiver(serviceBusConnectionStringBuilder, entityPath);
                    return messageReceiver;
                }
                else
                {
                    // Connection string includes the entity so we're using that instead of the message pump settings
                    MessageReceiver messageReceiver = CreateReceiver(serviceBusConnectionStringBuilder, serviceBusConnectionStringBuilder.EntityPath);
                    return messageReceiver;
                }
            }
            else
            {
                LogSecurityEvent("Get Azure Service Bus connection via security token");
                string entityPath = GetEntityPath();
                var messageReceiver = new MessageReceiver(Endpoint, entityPath, _tokenProvider);
                
                return messageReceiver;
            }
        }

        /// <summary>
        /// Creates an authenticated <see cref="ManagementClient"/> instance to manage settings on the Azure Service Bus resource.
        /// </summary>
        internal async Task<ManagementClient> CreatesManagementClientAsync()
        {
            if (_tokenProvider is null)
            {
                string connectionString = await GetConnectionStringAsync();
                var serviceBusConnectionBuilder = new ServiceBusConnectionStringBuilder(connectionString);
                
                var serviceBusClient = new ManagementClient(serviceBusConnectionBuilder);
                return serviceBusClient;
            }
            else
            {
                var serviceBusClient = new ManagementClient(Endpoint, _tokenProvider);
                return serviceBusClient;
            }
        }
        
        /// <summary>
        /// Gets the configured connection string.
        /// </summary>
        /// <returns>Connection string to authenticate with</returns>
        internal async Task<string> GetConnectionStringAsync()
        {
            _logger.LogTrace("Getting Azure Service Bus Topic connection string on topic '{TopicPath}'...",  EntityName);
            LogSecurityEvent("Get Azure Service Bus connection string");

            string connectionString;
            if (_getConnectionStringFromSecretFunc is null)
            {
                connectionString = GetConnectionStringFromConfiguration();
            }
            else
            {
                connectionString = await GetConnectionStringFromSecretAsync();
            }

            _logger.LogTrace("Got Azure Service Bus Topic connection string on topic '{TopicPath}'",  EntityName);
            return connectionString;
        }

        private void LogSecurityEvent(string eventName)
        {
            if (Options.EmitSecurityEvents)
            {
                _logger.LogSecurityEvent(eventName, new Dictionary<string, object>
                {
                    ["Service Bus entity"] = ServiceBusEntity,
                    ["Entity name"] = EntityName,
                    ["Job ID"] = Options.JobId
                });
            }
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
        
        private static MessageReceiver CreateReceiver(ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder, string entityPath)
        {
            string connectionString = serviceBusConnectionStringBuilder.GetNamespaceConnectionString();
            return new MessageReceiver(connectionString, entityPath);
        }

        internal string GetEntityPath()
        {
            if (string.IsNullOrWhiteSpace(SubscriptionName))
            {
                return EntityName;
            }

            return $"{EntityName}/subscriptions/{SubscriptionName}";
        }
    }
}
