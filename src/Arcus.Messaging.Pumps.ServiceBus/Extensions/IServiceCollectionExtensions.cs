using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Messaging.Pumps.ServiceBus.MessageHandling;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to add a <see cref="AzureServiceBusMessagePump"/> and its <see cref="IAzureServiceBusMessageHandler{TMessage}"/>'s implementations.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static partial class IServiceCollectionExtensions
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
        public static IServiceCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump(
                services,
                entityName: null,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureQueueMessagePump: configureMessagePump);

            return services;
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
        public static IServiceCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump(
                services,
                entityName: null,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureQueueMessagePump: configureMessagePump);

            return services;
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
        public static IServiceCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string secretName,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump(
                services,
                entityName: null,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureQueueMessagePump: configureMessagePump);

            return services;
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
        public static IServiceCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            string secretName,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump(
                services,
                entityName: queueName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureQueueMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump(
                services,
                entityName: queueName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureQueueMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue
        /// </summary>
        /// <param name="queueName">Name of the queue to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusQueueMessagePump(
                services,
                entityName: queueName,
                getConnectionStringFromConfigurationFunc,
                configureQueueMessagePump: configureMessagePump);

            return services;
        }

        private static void AddServiceBusQueueMessagePump(
            IServiceCollection services,
            string entityName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<AzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump(
                services,
                entityName,
                subscriptionName: null,
                ServiceBusEntity.Queue,
                configureQueueMessagePump: configureQueueMessagePump,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc);
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
        public static IServiceCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            string secretName,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusTopicMessagePump(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);


            return services;
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
        public static IServiceCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusTopicMessagePump(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);


            return services;
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
        public static IServiceCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusTopicMessagePump(
                services,
                entityName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
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
        public static IServiceCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string secretName,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusTopicMessagePump(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return services;
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
        public static IServiceCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusTopicMessagePump(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
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
        public static IServiceCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusTopicMessagePump(
                services,
                entityName: topicName,
                subscriptionName,
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
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
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            string secretName,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: null,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return services;
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
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: null,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
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
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: null,
                subscriptionPrefix: subscriptionPrefix,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="secretName">
        /// Name of the secret to retrieve using your registered <see cref="ISecretProvider" />
        /// implementation
        /// </param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            string secretName,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionPrefix">
        /// Prefix of the subscription to process, concat with the
        /// <see cref="AzureServiceBusMessagePumpConfiguration.JobId" />
        /// </param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<AzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNullOrWhitespace(subscriptionPrefix, nameof(subscriptionPrefix));

            AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return services;
        }

        private static void AddServiceBusTopicMessagePumpWithPrefix(
            IServiceCollection services,
            string entityName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<AzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
        {
            var messagePumpOptions = AzureServiceBusTopicMessagePumpOptions.Default;
            string subscriptionName = $"{subscriptionPrefix}-{messagePumpOptions.JobId}";

            AddServiceBusTopicMessagePump(
                services,
                entityName: entityName,
                subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureTopicMessagePump);
        }

        private static void AddServiceBusTopicMessagePump(
            IServiceCollection services,
            string entityName,
            string subscriptionName = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<AzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump(
                services,
                entityName,
                subscriptionName,
                ServiceBusEntity.Topic,
                configureTopicMessagePump: configureTopicMessagePump,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc);
        }

        private static void AddServiceBusMessagePump(
            IServiceCollection services,
            string entityName,
            string subscriptionName,
            ServiceBusEntity serviceBusEntity,
            Action<AzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump = null,
            Action<AzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null)
        {
            Guard.NotNull(services, nameof(services));

            services.AddCorrelation<MessageCorrelationInfo>();

            AzureServiceBusMessagePumpConfiguration options = 
                DetermineAzureServiceBusMessagePumpOptions(serviceBusEntity, configureQueueMessagePump, configureTopicMessagePump);

            services.WithServiceBusMessageRouting();
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
        }

        private static AzureServiceBusMessagePumpConfiguration DetermineAzureServiceBusMessagePumpOptions(
            ServiceBusEntity serviceBusEntity,
            Action<AzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump,
            Action<AzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump)
        {
            switch (serviceBusEntity)
            {
                case ServiceBusEntity.Queue:
                    var queueMessagePumpOptions = AzureServiceBusQueueMessagePumpOptions.Default;
                    configureQueueMessagePump?.Invoke(queueMessagePumpOptions);

                    return new AzureServiceBusMessagePumpConfiguration(queueMessagePumpOptions);
                
                case ServiceBusEntity.Topic:
                    var topicMessagePumpOptions = AzureServiceBusTopicMessagePumpOptions.Default;
                    configureTopicMessagePump?.Invoke(topicMessagePumpOptions);
                
                    return new AzureServiceBusMessagePumpConfiguration(topicMessagePumpOptions);
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceBusEntity), serviceBusEntity, "Unknown Azure Service Bus entity");
            }
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusMessageRouting(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to register the Azure Service Bus message routing");

            return services.WithServiceBusMessageRouting(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<AzureServiceBusMessageRouter>>();
                return new AzureServiceBusMessageRouter(serviceProvider, logger);
            });
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusMessageRouter"/> implementation to route the incoming messages through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.
        /// </summary>
        /// <param name="services">The collection of services to add the router to.</param>
        /// <param name="implementationFactory">The function to create the <typeparamref name="TMessageRouter"/> implementation.</param>
        /// <typeparam name="TMessageRouter">The type of the <see cref="IAzureServiceBusMessageRouter"/> implementation.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusMessageRouting<TMessageRouter>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageRouter> implementationFactory)
            where TMessageRouter : IAzureServiceBusMessageRouter
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to register the Azure Service Bus message routing");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the Azure Service Bus message router");

            return services.AddSingleton<IAzureServiceBusMessageRouter>(serviceProvider => implementationFactory(serviceProvider))
                           .WithMessageRouting(serviceProvider => serviceProvider.GetRequiredService<IAzureServiceBusMessageRouter>());
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(this IServiceCollection services)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");

            return services.AddTransient<IMessageHandler<TMessage, AzureServiceBusMessageContext>, TMessageHandler>();
        }

        /// <summary>
        /// Adds a <see cref="IAzureServiceBusMessageHandler{TMessage}" /> implementation to process the messages from Azure Service Bus
        /// resources.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the implementation.</typeparam>
        /// <typeparam name="TMessage">The type of the message that the message handler will process.</typeparam>
        /// <param name="services">The collection of services to use in the application.</param>
        /// <param name="implementationFactory">The function that creates the message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusMessageHandler<TMessageHandler, TMessage>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageHandler> implementationFactory)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the message handler");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires a function to create the message handler with dependent services");

            return services.AddTransient<IMessageHandler<TMessage, AzureServiceBusMessageContext>, TMessageHandler>(implementationFactory);
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="services">The services to add the fallback message handler to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusFallbackMessageHandler<TMessageHandler>(
            this IServiceCollection services)
            where TMessageHandler : class, IAzureServiceBusFallbackMessageHandler
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the Azure Service Bus fallback message handler to");

            return services.AddTransient<IAzureServiceBusFallbackMessageHandler, TMessageHandler>();
        }

        /// <summary>
        /// Adds an <see cref="IAzureServiceBusFallbackMessageHandler"/> implementation which the message pump can use to fall back to when no message handler is found to process the message.
        /// </summary>
        /// <typeparam name="TMessageHandler">The type of the fallback message handler.</typeparam>
        /// <param name="services">The services to add the fallback message handler to.</param>
        /// <param name="createImplementation">The function to create the fallback message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="createImplementation"/> is <c>null</c>.</exception>
        public static IServiceCollection WithServiceBusFallbackMessageHandler<TMessageHandler>(
            this IServiceCollection services,
            Func<IServiceProvider, TMessageHandler> createImplementation)
            where TMessageHandler : class, IAzureServiceBusFallbackMessageHandler
        {
            Guard.NotNull(services, nameof(services), "Requires a services collection to add the fallback message handler to");
            Guard.NotNull(createImplementation, nameof(createImplementation), "Requires a function to create the fallback message handler");

            return services.AddTransient<IAzureServiceBusFallbackMessageHandler, TMessageHandler>(createImplementation);
        }
    }
}