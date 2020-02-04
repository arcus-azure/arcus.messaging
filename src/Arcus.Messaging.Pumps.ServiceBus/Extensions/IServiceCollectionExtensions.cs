using System;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    // ReSharper disable once InconsistentNaming²
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>When using this approach; the connection string should be scoped to the queue that is being processed, not the namespace</remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(this IServiceCollection services, Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>When using this approach; the connection string should be scoped to the queue that is being processed, not the namespace</remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(this IServiceCollection services, Func<IConfiguration, string> getConnectionStringFromConfigurationFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>When using this approach; the connection string should be scoped to the queue that is being processed, not the namespace</remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="secretName">Name of the secret to retrieve using your registered <see cref="ISecretProvider"/> implementation</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(this IServiceCollection services, string queueName, string secretName, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, queueName, string.Empty, getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName), configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(this IServiceCollection services, string queueName, Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, queueName, string.Empty, getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(this IServiceCollection services, string queueName, Func<IConfiguration, string> getConnectionStringFromConfigurationFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, queueName, string.Empty, getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace</remarks>
        /// <param name="subscriptionPrefix">Prefix of the subscription to process, concat with the <see cref="AzureServiceBusMessagePumpOptions.JobId"/></param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">Name of the secret to retrieve using your registered <see cref="ISecretProvider"/> implementation</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(this IServiceCollection services, string subscriptionPrefix, string secretName, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, subscriptionPrefix: subscriptionPrefix, getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName), configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace</remarks>
        /// <param name="subscriptionPrefix">Prefix of the subscription to process, concat with the <see cref="AzureServiceBusMessagePumpOptions.JobId"/></param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(this IServiceCollection services, string subscriptionPrefix, Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, subscriptionPrefix: subscriptionPrefix, getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace</remarks>
        /// <param name="subscriptionPrefix">Prefix of the subscription to process, concat with the <see cref="AzureServiceBusMessagePumpOptions.JobId"/></param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(this IServiceCollection services, string subscriptionPrefix, Func<IConfiguration, string> getConnectionStringFromConfigurationFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, subscriptionPrefix: subscriptionPrefix, getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">Prefix of the subscription to process, concat with the <see cref="AzureServiceBusMessagePumpOptions.JobId"/></param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">Name of the secret to retrieve using your registered <see cref="ISecretProvider"/> implementation</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(this IServiceCollection services, string topicName, string subscriptionPrefix, string secretName, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, topicName, subscriptionPrefix, getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName), configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">Prefix of the subscription to process, concat with the <see cref="AzureServiceBusMessagePumpOptions.JobId"/></param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(this IServiceCollection services, string topicName, string subscriptionPrefix, Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, topicName, subscriptionPrefix, getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">Prefix of the subscription to process, concat with the <see cref="AzureServiceBusMessagePumpOptions.JobId"/></param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(this IServiceCollection services, string topicName, string subscriptionPrefix, Func<IConfiguration, string> getConnectionStringFromConfigurationFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<TMessagePump>(services, topicName, subscriptionPrefix, getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        private static void AddServiceBusMessagePump<TMessagePump>(IServiceCollection services, string entityName = null, string subscriptionPrefix = null, Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null, Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            var messagePumpOptions = AzureServiceBusMessagePumpOptions.Default;
            configureMessagePump?.Invoke(messagePumpOptions);

            services.AddSingleton(serviceProvider => new AzureServiceBusMessagePumpSettings(entityName, subscriptionPrefix, getConnectionStringFromConfigurationFunc, getConnectionStringFromSecretFunc, messagePumpOptions, serviceProvider));
            services.AddHostedService<TMessagePump>();
        }
    }
}