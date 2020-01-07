using System;
using System.Threading.Tasks;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    public class AzureServiceBusMessagePumpSettings
    {
        private readonly Func<ISecretProvider, Task<string>> _getConnectionStringFromSecretFunc;
        private readonly Func<IConfiguration, string> _getConnectionStringFromConfigurationFunc;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="entityName">Name of the entity to process</param>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="options">Options that influence the behavior of the message pump</param>
        /// <param name="serviceProvider">Collection of services to use</param>
        public AzureServiceBusMessagePumpSettings(string entityName, string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            AzureServiceBusMessagePumpOptions options, IServiceProvider serviceProvider)
        {
            Guard.For<ArgumentException>(
                () => getConnectionStringFromConfigurationFunc == null && getConnectionStringFromSecretFunc == null,
                "Unable to determine connection string as it was not defined how to look it up");
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(serviceProvider, nameof(serviceProvider));

            _serviceProvider = serviceProvider;
            _getConnectionStringFromConfigurationFunc = getConnectionStringFromConfigurationFunc;
            _getConnectionStringFromSecretFunc = getConnectionStringFromSecretFunc;

            EntityName = entityName;
            SubscriptionName = subscriptionName;
            Options = options;
        }

        /// <summary>
        ///     Name of the entity to process
        /// </summary>
        /// <remarks>This is optional as the connection string can contain the entity name</remarks>
        public string EntityName { get; set; }

        /// <summary>
        ///     Name of the subscription to process
        /// </summary>
        /// <remarks>This is only applicable when using Azure Service Bus Topics</remarks>
        public string SubscriptionName { get; set; }

        /// <summary>
        ///     Options that influence the behavior of the message pump
        /// </summary>
        public AzureServiceBusMessagePumpOptions Options { get; set; }

        /// <summary>
        ///     Gets the configured connection string
        /// </summary>
        /// <returns>Connection string to authenticate with</returns>
        public async Task<string> GetConnectionStringAsync()
        {
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
            var secretProvider = _serviceProvider.GetRequiredService<ISecretProvider>();
            return await _getConnectionStringFromSecretFunc(secretProvider);
        }
    }
}