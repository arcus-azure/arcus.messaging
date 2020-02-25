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
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the queue that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: null,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the queue that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: null,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the queue that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            string secretName,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: null,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the queue that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            string queueName,
            string secretName,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: queueName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureMessagePump: configureMessagePump);

            return services;
        }

       

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            string queueName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: queueName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessagePump>(
            this IServiceCollection services,
            string queueName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump<TMessagePump>(
                services,
                entityName: queueName,
                getConnectionStringFromConfigurationFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        private static void AddServiceBusQueueMessagePump<TMessagePump>(
            IServiceCollection services,
            string entityName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            var messagePumpOptions = AzureServiceBusMessagePumpOptions.Default;
            configureMessagePump?.Invoke(messagePumpOptions);

            AddServiceBusMessagePump<TMessagePump>(
                services,
                entityName,
                subscriptionName: null,
                ServiceBusEntity.Queue,
                messagePumpOptions,
                getConnectionStringFromConfigurationFunc,
                getConnectionStringFromSecretFunc);
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionName">Name of the subscription to process </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string subscriptionName,
            string secretName,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionName">Name of the subscription to process </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionName">Name of the subscription to process </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">
        /// Name of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string secretName,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName));

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromConfigurationFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string subscriptionPrefix,
            string secretName,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: null,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: null,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: null,
                subscriptionPrefix: subscriptionPrefix,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            string secretName,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromConfigurationFunc,
                configureMessagePump: configureMessagePump);

            return services;
        }

        private static void AddServiceBusTopicMessagePumpWithPrefix<TMessagePump>(
            IServiceCollection services,
            string entityName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            var messagePumpOptions = AzureServiceBusMessagePumpOptions.Default;
            string subscriptionName = $"{subscriptionPrefix}-{messagePumpOptions.JobId}";

            AddServiceBusTopicMessagePump<TMessagePump>(
                services,
                entityName: entityName,
                subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureMessagePump: configureMessagePump);
        }

        private static void AddServiceBusTopicMessagePump<TMessagePump>(
            IServiceCollection services,
            string entityName,
            string subscriptionName = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            var messagePumpOptions = AzureServiceBusMessagePumpOptions.Default;
            configureMessagePump?.Invoke(messagePumpOptions);

            AddServiceBusMessagePump<TMessagePump>(
                services,
                entityName,
                subscriptionName,
                ServiceBusEntity.Topic,
                messagePumpOptions,
                getConnectionStringFromConfigurationFunc,
                getConnectionStringFromSecretFunc);
        }

        private static void AddServiceBusMessagePump<TMessagePump>(
            IServiceCollection services,
            string entityName,
            string subscriptionName,
            ServiceBusEntity serviceBusEntity,
            AzureServiceBusMessagePumpOptions messagePumpOptions,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null)
            where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            services.AddSingleton(serviceProvider =>
            {
                return new AzureServiceBusMessagePumpSettings(
                    entityName,
                    subscriptionName,
                    serviceBusEntity,
                    getConnectionStringFromConfigurationFunc,
                    getConnectionStringFromSecretFunc,
                    messagePumpOptions,
                    serviceProvider);
            });
            services.AddHostedService<TMessagePump>();
        }
    }
}