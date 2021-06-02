using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add a <see cref="AzureServiceBusMessagePump"/> and its <see cref="IAzureServiceBusMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the queue that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusQueueMessagePump(
                services,
                entityName: null,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the queue that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusQueueMessagePump(
                services,
                entityName: null,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue
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
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string secretName,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusQueueMessagePump(
                services,
                entityName: null,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue
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
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            string secretName,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusQueueMessagePump(
                services,
                entityName: queueName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusQueueMessagePump(
                services,
                entityName: queueName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusQueueMessagePump(
                services,
                entityName: queueName,
                getConnectionStringFromConfigurationFunc,
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            IServiceCollection services,
            string entityName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusMessagePump(
                services,
                entityName,
                subscriptionName: null,
                ServiceBusEntityType.Queue,
                configureQueueMessagePump: configureQueueMessagePump,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            string secretName,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);


            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string secretName,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            string secretName,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            var collection = AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: null,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            var collection = AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: null,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>
        /// When using this approach; the connection string should be scoped to the topic that is being processed, not the
        /// namespace
        /// </remarks>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            var collection = AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: null,
                subscriptionPrefix: subscriptionPrefix,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            string secretName,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            var collection = AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            var collection = AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            var collection = AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            IServiceCollection services,
            string entityName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
        {
            var messagePumpOptions = AzureServiceBusMessagePumpOptions.DefaultTopicOptions;
            string subscriptionName = $"{subscriptionPrefix}-{messagePumpOptions.JobId}";

            ServiceBusMessageHandlerCollection collection = AddServiceBusTopicMessagePump(
                services,
                entityName: entityName,
                subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureTopicMessagePump);

            return collection;
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            IServiceCollection services,
            string entityName,
            string subscriptionName = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            ServiceBusMessageHandlerCollection collection = AddServiceBusMessagePump(
                services,
                entityName,
                subscriptionName,
                ServiceBusEntityType.Topic,
                configureTopicMessagePump: configureTopicMessagePump,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc);

            return collection;
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusMessagePump(
            IServiceCollection services,
            string entityName,
            string subscriptionName,
            ServiceBusEntityType serviceBusEntity,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null)
        {
            Guard.NotNull(services, nameof(services));

            services.AddCorrelation<MessageCorrelationInfo>();

            AzureServiceBusMessagePumpOptions options = 
                DetermineAzureServiceBusMessagePumpOptions(serviceBusEntity, configureQueueMessagePump, configureTopicMessagePump);

            ServiceBusMessageHandlerCollection collection = services.AddServiceBusMessageRouting();
            services.AddHostedService(serviceProvider =>
            {
                var settings = new AzureServiceBusMessagePumpSettings(
                    entityName,
                    subscriptionName,
                    serviceBusEntity,
                    getConnectionStringFromConfigurationFunc,
                    getConnectionStringFromSecretFunc,
                    options,
                    serviceProvider);

                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var router = serviceProvider.GetService<IAzureServiceBusMessageRouter>();
                var logger = serviceProvider.GetRequiredService<ILogger<AzureServiceBusMessagePump>>();
                return new AzureServiceBusMessagePump(settings, configuration, serviceProvider, router, logger);
            });

            return collection;
        }

        private static AzureServiceBusMessagePumpOptions DetermineAzureServiceBusMessagePumpOptions(
            ServiceBusEntityType serviceBusEntity,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump)
        {
            switch (serviceBusEntity)
            {
                case ServiceBusEntityType.Queue:
                    var queueMessagePumpOptions = AzureServiceBusMessagePumpOptions.DefaultQueueOptions;
                    configureQueueMessagePump?.Invoke(queueMessagePumpOptions);

                    return queueMessagePumpOptions;
                
                case ServiceBusEntityType.Topic:
                    var topicMessagePumpOptions = AzureServiceBusMessagePumpOptions.DefaultTopicOptions;
                    configureTopicMessagePump?.Invoke(topicMessagePumpOptions);

                    return topicMessagePumpOptions;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceBusEntity), serviceBusEntity, "Unknown Azure Service Bus entity");
            }
        }
    }
}