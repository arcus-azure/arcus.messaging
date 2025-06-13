using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            {
                throw new ArgumentException("Requires a non-blank fully-qualified namespace for the Azure Service bus message pump registration", nameof(fullyQualifiedNamespace));
            }

            if (credential is null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

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
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException("Requires a non-blank queue name for the Azure Service bus message pump registration", nameof(queueName));
            }

            if (clientImplementationFactory is null)
            {
                throw new ArgumentNullException(nameof(clientImplementationFactory));
            }

            ServiceBusMessageHandlerCollection collection =
                AddServiceBusMessagePump(services, CreateSettings, configureMessagePump);

            return collection;

            AzureServiceBusMessagePumpSettings CreateSettings(IServiceProvider serviceProvider, AzureServiceBusMessagePumpOptions options)
            {
                return new AzureServiceBusMessagePumpSettings(
                    queueName,
                    subscriptionName: null,
                    ServiceBusEntityType.Queue,
                    clientImplementationFactory,
                    clientAdminImplementationFactory: null,
                    options,
                    serviceProvider);
            }
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
            if (string.IsNullOrWhiteSpace(fullyQualifiedNamespace))
            {
                throw new ArgumentException("Requires a non-blank fully-qualified Azure Service bus namespace for the message pump registration", nameof(fullyQualifiedNamespace));
            }

            if (credential is null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

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
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Requires a non-blank topic name for the Azure Service bus message pump registration", nameof(topicName));
            }

            if (string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentException("Requires a non-blank subscription name for the Azure Service bus message pump registration", nameof(subscriptionName));
            }

            if (clientImplementationFactory is null)
            {
                throw new ArgumentNullException(nameof(clientImplementationFactory));
            }

            ServiceBusMessageHandlerCollection collection = AddServiceBusMessagePump(
                services,
                CreateSettings,
                configureMessagePump);

            return collection;

            AzureServiceBusMessagePumpSettings CreateSettings(IServiceProvider serviceProvider, AzureServiceBusMessagePumpOptions options)
            {
                return new AzureServiceBusMessagePumpSettings(
                    topicName,
                    subscriptionName,
                    ServiceBusEntityType.Topic,
                    clientImplementationFactory,
                    clientAdminImplementationFactory: null,
                    options,
                    serviceProvider);
            }
        }

        private static Func<IServiceProvider, ServiceBusClient> CreateClientFactory(string fullyQualifiedNamespace, TokenCredential credential)
        {
            string SanitizeServiceBusNamespace(string serviceBusNamespace)
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
            Func<IServiceProvider, AzureServiceBusMessagePumpOptions, AzureServiceBusMessagePumpSettings> createSettings,
            Action<AzureServiceBusMessagePumpOptions> configureOptions = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var options = AzureServiceBusMessagePumpOptions.DefaultOptions;
            configureOptions?.Invoke(options);

#pragma warning disable CS0618 // Type or member is obsolete
            ServiceBusMessageHandlerCollection collection = services.AddServiceBusMessageRouting(provider =>
            {
                var logger = provider.GetService<ILogger<AzureServiceBusMessageRouter>>();
                return new AzureServiceBusMessageRouter(provider, options.Routing, logger);
            });
#pragma warning restore CS0618 // Type or member is obsolete
            collection.JobId = options.JobId;

            services.TryAddSingleton<IMessagePumpCircuitBreaker>(provider => new DefaultMessagePumpCircuitBreaker(provider, provider.GetService<ILogger<DefaultMessagePumpCircuitBreaker>>()));

            services.AddHostedService(provider =>
            {
                var router = provider.GetService<IAzureServiceBusMessageRouter>();
                var logger = provider.GetService<ILogger<AzureServiceBusMessagePump>>();

                AzureServiceBusMessagePumpSettings settings = createSettings(provider, options);
                return new AzureServiceBusMessagePump(settings, provider, router, logger);
            });

            return collection;
        }
    }
}