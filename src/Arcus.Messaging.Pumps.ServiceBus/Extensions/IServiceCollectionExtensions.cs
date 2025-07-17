using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        /// Adds a message pump to consume messages from an Azure Service bus queue.
        /// </summary>
        /// <param name="services">The collection of application services to add the message pump to.</param>
        /// <param name="queueName">The name of the Azure Service bus queue resource.</param>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Azure Service Bus namespace to connect to.
        ///     This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="credential">The credentials implementation to authenticate with the Azure Service bus resource.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            string fullyQualifiedNamespace,
            TokenCredential credential)
        {
            return AddServiceBusQueueMessagePump(services, queueName, fullyQualifiedNamespace, credential, configureMessagePump: null);
        }

        /// <summary>
        /// Adds a message pump to consume messages from an Azure Service bus queue.
        /// </summary>
        /// <param name="services">The collection of application services to add the message pump to.</param>
        /// <param name="queueName">The name of the Azure Service bus queue resource.</param>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Azure Service Bus namespace to connect to.
        ///     This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="credential">The credentials implementation to authenticate with the Azure Service bus resource.</param>
        /// <param name="configureMessagePump">The optional function to manipulate the behavior of the message pump.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            string fullyQualifiedNamespace,
            TokenCredential credential,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedNamespace);
            ArgumentNullException.ThrowIfNull(credential);

            return AddServiceBusQueueMessagePump(services, queueName, CreateClientFactory(fullyQualifiedNamespace, credential), configureMessagePump);
        }

        /// <summary>
        /// Adds a message pump to consume messages from an Azure Service bus queue.
        /// </summary>
        /// <param name="services">The collection of application services to add the message pump to.</param>
        /// <param name="queueName">The name of the Azure Service bus queue resource.</param>
        /// <param name="clientImplementationFactory">The factory function to create an operation client towards the Azure Service bus resource.</param>
        /// <param name="configureMessagePump">The optional function to manipulate the behavior of the message pump.</param>
        /// <exception cref="ArgumentException"></exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            Func<IServiceProvider, ServiceBusClient> clientImplementationFactory,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
            ArgumentNullException.ThrowIfNull(clientImplementationFactory);

            return AddServiceBusMessagePump(services, queueName, clientImplementationFactory, ServiceBusEntityType.Queue, configureMessagePump);
        }

        /// <summary>
        /// Adds a message pump to consume messages from an Azure Service bus topic subscription.
        /// </summary>
        /// <param name="services">The collection of application services to add the message pump to.</param>
        /// <param name="topicName">The name of the Azure Service bus topic resource.</param>
        /// <param name="subscriptionName">The name of the Azure Service bus topic subscription to process.</param>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Azure Service bus namespace to connect to.
        ///     This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="credential">The credentials implementation to authenticate with the Azure Service bus resource.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> is blank.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string fullyQualifiedNamespace,
            TokenCredential credential)
        {
            return AddServiceBusTopicMessagePump(services, topicName, subscriptionName, fullyQualifiedNamespace, credential, configureMessagePump: null);
        }

        /// <summary>
        /// Adds a message pump to consume messages from an Azure Service bus topic subscription.
        /// </summary>
        /// <param name="services">The collection of application services to add the message pump to.</param>
        /// <param name="topicName">The name of the Azure Service bus topic resource.</param>
        /// <param name="subscriptionName">The name of the Azure Service bus topic subscription to process.</param>
        /// <param name="fullyQualifiedNamespace">
        ///     The fully qualified Azure Service bus namespace to connect to.
        ///     This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </param>
        /// <param name="credential">The credentials implementation to authenticate with the Azure Service bus resource.</param>
        /// <param name="configureMessagePump">The optional function to manipulate the behavior of the message pump.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> is blank.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string fullyQualifiedNamespace,
            TokenCredential credential,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedNamespace);
            ArgumentNullException.ThrowIfNull(credential);

            return AddServiceBusTopicMessagePump(services, topicName, subscriptionName, CreateClientFactory(fullyQualifiedNamespace, credential), configureMessagePump);
        }

        /// <summary>
        /// Adds a message pump to consume messages from an Azure Service bus topic subscription.
        /// </summary>
        /// <param name="services">The collection of application services to add the message pump to.</param>
        /// <param name="topicName">The name of the Azure Service bus topic resource.</param>
        /// <param name="subscriptionName">The name of the Azure Service bus topic subscription to process.</param>
        /// <param name="clientImplementationFactory">The factory function to create an operation client towards the Azure Service bus resource.</param>
        /// <param name="configureMessagePump">The optional function to manipulate the behavior of the message pump.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> is blank.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<IServiceProvider, ServiceBusClient> clientImplementationFactory,
            Action<AzureServiceBusMessagePumpOptions> configureMessagePump)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
            ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
            ArgumentNullException.ThrowIfNull(clientImplementationFactory);

            return AddServiceBusMessagePump(
                services,
                topicName,
                clientImplementationFactory,
                ServiceBusEntityType.Topic,
                configureMessagePump,
                subscriptionName);
        }

        /// <summary>
        /// Adds a message pump to consume messages with session-support from an Azure Service bus topic subscription.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="topicName"></param>
        /// <param name="subscriptionName"></param>
        /// <param name="fullyQualifiedNamespace"></param>
        /// <param name="credential"></param>
        /// <param name="configureMessagePump"></param>
        /// <returns></returns>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithSessionSupport(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string fullyQualifiedNamespace,
            TokenCredential credential,
            Action<AzureServiceBusSessionMessagePumpOptions> configureMessagePump)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
            ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
            ArgumentNullException.ThrowIfNull(credential);

            ServiceBusMessageHandlerCollection collection = AddServiceBusSessionMessagePump(
                services,
                CreateSettings,
                configureMessagePump);

            return collection;

            AzureServiceBusSessionMessagePumpSettings CreateSettings(IServiceProvider serviceProvider, AzureServiceBusSessionMessagePumpOptions options)
            {
                return new AzureServiceBusSessionMessagePumpSettings(
                    topicName,
                    subscriptionName,
                    ServiceBusEntityType.Topic,
                    CreateClientFactory(fullyQualifiedNamespace, credential),
                    options,
                    serviceProvider);
            }
        }

        private static Func<IServiceProvider, ServiceBusClient> CreateClientFactory(string fullyQualifiedNamespace, TokenCredential credential)
        {
            static string SanitizeServiceBusNamespace(string serviceBusNamespace)
            {
                if (!serviceBusNamespace.EndsWith(".servicebus.windows.net"))
                {
                    serviceBusNamespace += ".servicebus.windows.net";
                }

                return serviceBusNamespace;
            }

            return _ => new ServiceBusClient(SanitizeServiceBusNamespace(fullyQualifiedNamespace), credential);
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusMessagePump(
            IServiceCollection services,
            string entityPath,
            Func<IServiceProvider, ServiceBusClient> clientImplementationFactory,
            ServiceBusEntityType entityType,
            Action<AzureServiceBusMessagePumpOptions> configureOptions = null,
            string subscriptionName = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            var options = new AzureServiceBusMessagePumpOptions();
            configureOptions?.Invoke(options);

            services.AddApplicationInsightsTelemetryWorkerService();

            services.TryAddSingleton<IMessagePumpCircuitBreaker>(provider =>
            {
                var logger = provider.GetService<ILogger<DefaultMessagePumpCircuitBreaker>>();
                return new DefaultMessagePumpCircuitBreaker(provider, logger);
            });

            services.AddHostedService(provider =>
            {
                subscriptionName = SanitizeSubscriptionName(subscriptionName, provider);
                var logger = provider.GetService<ILogger<AzureServiceBusMessagePump>>();

                return new AzureServiceBusMessagePump(entityPath, subscriptionName, entityType, clientImplementationFactory, options, provider, logger);
            });

            return new ServiceBusMessageHandlerCollection(services) { JobId = options.JobId };
        }

        private static string SanitizeSubscriptionName(string subscriptionName, IServiceProvider provider)
        {
            var logger =
                provider.GetService<ILogger<AzureServiceBusMessagePump>>()
                ?? NullLogger<AzureServiceBusMessagePump>.Instance;

            if (subscriptionName is { Length: > 50 })
            {
                logger.LogWarning("Azure Service Bus Topic subscription name was truncated to 50 characters");
                subscriptionName = subscriptionName[..50];
            }

            return subscriptionName;
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusSessionMessagePump(IServiceCollection services,
            Func<IServiceProvider, AzureServiceBusSessionMessagePumpOptions, AzureServiceBusSessionMessagePumpSettings> createSettings,
            Action<AzureServiceBusSessionMessagePumpOptions> configureOptions = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            var options = AzureServiceBusSessionMessagePumpOptions.DefaultOptions;
            configureOptions?.Invoke(options);

            services.AddApplicationInsightsTelemetryWorkerService();

            services.TryAddSingleton<IAzureServiceBusMessageRouter>(provider =>
            {
                var logger = provider.GetService<ILogger<AzureServiceBusMessageRouter>>();
                return new AzureServiceBusMessageRouter(provider, options.Routing, logger);
            });

            // Circuitbreaker is not supported on a MessagePump that supports sessions.
            //services.TryAddSingleton<IMessagePumpCircuitBreaker>(provider => new DefaultMessagePumpCircuitBreaker(provider, provider.GetService<ILogger<DefaultMessagePumpCircuitBreaker>>()));

            services.AddHostedService(provider =>
            {
                var router = provider.GetService<IAzureServiceBusMessageRouter>();
                var logger = provider.GetService<ILogger<AzureServiceBusSessionMessagePump>>();

                AzureServiceBusSessionMessagePumpSettings settings = createSettings(provider, options);
                return new AzureServiceBusSessionMessagePump(settings, provider, router, logger);
            });

            return new ServiceBusMessageHandlerCollection(services) { JobId = options.JobId };
        }
    }
}