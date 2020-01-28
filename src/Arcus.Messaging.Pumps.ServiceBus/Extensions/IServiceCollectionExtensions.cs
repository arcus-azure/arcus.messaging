using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions;
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
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessage>(this IServiceCollection services, Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<AzureServiceBusMessagePump<TMessage>>(services, getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc, configureMessagePump: configureMessagePump);

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
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessage>(this IServiceCollection services, Func<IConfiguration, string> getConnectionStringFromConfigurationFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
            
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<AzureServiceBusMessagePump<TMessage>>(services, getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc, configureMessagePump: configureMessagePump);

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
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessage>(this IServiceCollection services, string queueName, Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<AzureServiceBusMessagePump<TMessage>>(services, queueName, string.Empty, getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc, configureMessagePump: configureMessagePump);

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
        public static IServiceCollection AddServiceBusQueueMessagePump<TMessage>(this IServiceCollection services, string queueName, Func<IConfiguration, string> getConnectionStringFromConfigurationFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<AzureServiceBusMessagePump<TMessage>>(services, queueName, string.Empty, getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace</remarks>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessage>(this IServiceCollection services, string subscriptionName, Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<AzureServiceBusMessagePump<TMessage>>(services, subscriptionName: subscriptionName, getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <remarks>When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace</remarks>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessage>(this IServiceCollection services, string subscriptionName, Func<IConfiguration, string> getConnectionStringFromConfigurationFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<AzureServiceBusMessagePump<TMessage>>(services, subscriptionName: subscriptionName, getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromSecretFunc">Function to look up the connection string from the secret store</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessage>(this IServiceCollection services, string topicName, string subscriptionName, Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<AzureServiceBusMessagePump<TMessage>>(services, topicName, subscriptionName, getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        /// <summary>
        ///     Adds a message handler to consume messages from Azure Service Bus Topic
        /// </summary>
        /// <param name="topicName">Name of the topic to work with</param>
        /// <param name="subscriptionName">Name of the subscription to process</param>
        /// <param name="services">Collection of services to use in the application</param>
        /// <param name="getConnectionStringFromConfigurationFunc">Function to look up the connection string from the configuration</param>
        /// <param name="configureMessagePump">Capability to configure how the message pump should behave</param>
        /// <returns>Collection of services to use in the application</returns>
        public static IServiceCollection AddServiceBusTopicMessagePump<TMessage>(this IServiceCollection services, string topicName, string subscriptionName, Func<IConfiguration, string> getConnectionStringFromConfigurationFunc, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services));

            AddServiceBusMessagePump<AzureServiceBusMessagePump<TMessage>>(services, topicName, subscriptionName, getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc, configureMessagePump: configureMessagePump);

            return services;
        }

        private static void AddServiceBusMessagePump<TMessagePump>(IServiceCollection services, string entityName = null, string subscriptionName = null, Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null, Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null, Action<AzureServiceBusMessagePumpOptions> configureMessagePump = null) where TMessagePump : class, IHostedService
        {
            Guard.NotNull(services, nameof(services));

            var messagePumpOptions = AzureServiceBusMessagePumpOptions.Default;
            configureMessagePump?.Invoke(messagePumpOptions);

            services.AddSingleton(serviceProvider => new AzureServiceBusMessagePumpSettings(entityName, subscriptionName, getConnectionStringFromConfigurationFunc, getConnectionStringFromSecretFunc, messagePumpOptions, serviceProvider));
            services.AddHostedService<TMessagePump>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <typeparam name="TMessageHandler"></typeparam>
        /// <param name="services"></param>
        /// <param name="messageHandlerPredicate"></param>
        /// <returns></returns>
        public static IServiceCollection WithMessagePumpHandler<TMessage, TMessageHandler>(
            this IServiceCollection services,
            Func<AzureServiceBusMessageContext, bool> messageHandlerPredicate)
            where TMessageHandler : class, IMessageHandler<TMessage, AzureServiceBusMessageContext>
        {
            Guard.NotNull(messageHandlerPredicate, nameof(messageHandlerPredicate));

            return WithMessagePumpHandler<TMessage, AzureServiceBusMessageContext, TMessageHandler>(services, messageHandlerPredicate);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <typeparam name="TMessageContext"></typeparam>
        /// <typeparam name="TMessageHandler"></typeparam>
        /// <param name="services"></param>
        /// <param name="messageHandlerPredicate"></param>
        /// <returns></returns>
        public static IServiceCollection WithMessagePumpHandler<TMessage, TMessageContext, TMessageHandler>(
            this IServiceCollection services,
            Func<TMessageContext, bool> messageHandlerPredicate)
            where TMessageContext : MessageContext
            where TMessageHandler : class, IMessageHandler<TMessage, TMessageContext>
        {
            Guard.NotNull(messageHandlerPredicate, nameof(messageHandlerPredicate));

            services.AddSingleton<IMessageHandler<TMessage, TMessageContext>, TMessageHandler>();

            return services;
        }
    }
}