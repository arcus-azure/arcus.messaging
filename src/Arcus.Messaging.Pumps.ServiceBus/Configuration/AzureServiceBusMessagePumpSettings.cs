using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
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
        ///     Name of the entity to process
        /// </summary>
        /// <remarks>This is optional as the connection string can contain the entity name</remarks>
        public string EntityName { get; }

        /// <summary>
        ///     Name of the subscription to process
        /// </summary>
        /// <remarks>This is only applicable when using Azure Service Bus Topics</remarks>
        public string SubscriptionName { get; }

        /// <summary>
        ///     Entity of the Service Bus.
        /// </summary>
        public ServiceBusEntityType ServiceBusEntity { get; }

        /// <summary>
        ///     Options that influence the behavior of the message pump
        /// </summary>
        public AzureServiceBusMessagePumpConfiguration Options { get; internal set; }

        /// <summary>
        ///     Gets the configured connection string
        /// </summary>
        /// <returns>Connection string to authenticate with</returns>
        public async Task<string> GetConnectionStringAsync()
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
