using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Security.Core;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

#pragma warning disable CS0618 // Type or member is obsolete: lots of functionality will be removed or made internal in v3.0.

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
        /// Adds a message pump to consume messages from Azure Service Bus Queue.
        /// </summary>
        /// <remarks>
        ///     <para>When using this approach; the connection string should be scoped to the queue that is being processed, not the namespace.</para>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="getConnectionStringFromSecretFunc">The function to look up the connection string scoped to the Azure Service Bus Queue from the secret store.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="getConnectionStringFromSecretFunc"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusQueueMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddDeprecatedServiceBusQueueMessagePumpWithConnectionString(
                services,
                entityName: null,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue.
        /// </summary>
        /// <remarks>
        ///     <para>When using this approach; the connection string should be scoped to the queue that is being processed, not the namespace.</para>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="getConnectionStringFromConfigurationFunc">The function to look up the connection string scoped to the Azure Service Bus Queue from the configuration.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="getConnectionStringFromConfigurationFunc"/> is <c>null</c>.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusQueueMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddDeprecatedServiceBusQueueMessagePumpWithConnectionString(
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
        ///     <para>When using this approach; the connection string should be scoped to the queue that is being processed, not the namespace.</para>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="secretName">
        ///     The name of the secret to retrieve the connection string scoped to the Azure Service Bus Queue using your registered <see cref="ISecretProvider" /> implementation.
        /// </param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusQueueMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string secretName,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Requires a non-blank secret name", nameof(secretName));
            }

            var collection = AddDeprecatedServiceBusQueueMessagePumpWithConnectionString(
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
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="queueName">The name of the Azure Service Bus Queue to process.</param>
        /// <param name="secretName">
        ///     Name of the secret to retrieve the connection string scoped to the Azure Service Bus Queue using your registered <see cref="ISecretProvider" /> implementation.
        /// </param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> or <paramref name="secretName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusQueueMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            string secretName,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(queueName));
            }

            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Requires a non-blank secret name", nameof(secretName));
            }

            var collection = AddDeprecatedServiceBusQueueMessagePumpWithConnectionString(
                services,
                entityName: queueName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="queueName">The name of the Azure Service Bus Queue to process.</param>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="getConnectionStringFromSecretFunc">The function to look up the connection string scoped to the Azure Service Bus Queue from the secret store.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or <paramref name="getConnectionStringFromSecretFunc"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusQueueMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(queueName));
            }

            var collection = AddDeprecatedServiceBusQueueMessagePumpWithConnectionString(
                services,
                entityName: queueName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue.
        /// </summary>
        /// <param name="queueName">The name of the Azure Service Bus Queue to process.</param>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="getConnectionStringFromConfigurationFunc">The function to look up the connection string scoped to the Azure Service Bus Queue from the configuration.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or <paramref name="getConnectionStringFromConfigurationFunc"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusQueueMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(queueName));
            }

            var collection = AddDeprecatedServiceBusQueueMessagePumpWithConnectionString(
                services,
                entityName: queueName,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        [Obsolete("Will be removed in v3.0")]
        private static ServiceBusMessageHandlerCollection AddDeprecatedServiceBusQueueMessagePumpWithConnectionString(
            IServiceCollection services,
            string entityName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump = null)
        {
            return AddServiceBusMessagePump(services, CreateSettings, configureQueueMessagePump);

            AzureServiceBusMessagePumpSettings CreateSettings(IServiceProvider serviceProvider, AzureServiceBusMessagePumpOptions options)
            {
                return new AzureServiceBusMessagePumpSettings(
                    entityName, subscriptionName: null, ServiceBusEntityType.Queue, getConnectionStringFromConfigurationFunc, getConnectionStringFromSecretFunc, options, serviceProvider);
            }
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Queue.
        /// </summary>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="queueName">The name of the Azure Service Bus Queue to process.</param>
        /// <param name="serviceBusNamespace">The Service Bus namespace to connect to.  This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.</param>
        /// <param name="clientId">
        ///     The client ID to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
        ///     <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm" />.
        /// </param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="queueName"/> or <paramref name="serviceBusNamespace"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusQueueMessagePump) + " overload instead that takes in a " + nameof(TokenCredential))]
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePumpUsingManagedIdentity(
            this IServiceCollection services,
            string queueName,
            string serviceBusNamespace,
            string clientId = null,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = clientId
            });

            return AddServiceBusQueueMessagePump(services, queueName, serviceBusNamespace, credential, configureMessagePump);
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
                AddServiceBusMessagePump(services, CreateSettings, ConvertQueueOptions(configureMessagePump));

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

        [Obsolete("Will be removed in v3.0")]
        private static Action<IAzureServiceBusQueueMessagePumpOptions> ConvertQueueOptions(Action<AzureServiceBusMessagePumpOptions> configureOptions)
        {
            return deprecatedOptions => configureOptions?.Invoke((AzureServiceBusMessagePumpOptions) deprecatedOptions);
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <remarks>
        ///     <para>When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace.</para>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="subscriptionName">The name of the Azure Service Bus Topic subscription to process.</param>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="secretName">
        ///     The name of the secret to retrieve the connection string scoped to the Azure Service Bus Topic using your registered <see cref="ISecretProvider" /> implementation.
        /// </param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionName"/> or <paramref name="secretName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            string secretName,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Requires a non-blank secret name", nameof(secretName));
            }

            var collection = AddDeprecatedServiceBusTopicMessagePumpWithConnectionString(
                services,
                topicName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);


            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <remarks>
        ///     <para>When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace.</para>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="subscriptionName">The name of the Azure Service Bus Topic subscription to process.</param>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="getConnectionStringFromSecretFunc">The function to look up the connection string scoped to the Azure Service Bus Topic from the secret store.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or the <paramref name="getConnectionStringFromSecretFunc"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddDeprecatedServiceBusTopicMessagePumpWithConnectionString(
                services,
                topicName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <remarks>
        ///     When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace.
        /// </remarks>
        /// <param name="subscriptionName">The name of the subscription to process.</param>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="getConnectionStringFromConfigurationFunc">The function to look up the connection string scoped to the Azure Service Bus Topic from the configuration.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or the <paramref name="getConnectionStringFromConfigurationFunc"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddDeprecatedServiceBusTopicMessagePumpWithConnectionString(
                services,
                topicName: null,
                subscriptionName: subscriptionName,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process.</param>
        /// <param name="subscriptionName">The name of the Azure Service Bus Topic subscription to process</param>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="secretName">
        ///     The name of the secret to retrieve the connection string using your registered <see cref="ISecretProvider" /> implementation.
        /// </param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="topicName"/>, the <paramref name="subscriptionName"/>, or the <paramref name="secretName"/> is blank.
        /// </exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string secretName,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(topicName));
            }

            var collection = AddDeprecatedServiceBusTopicMessagePumpWithConnectionString(
                services,
                topicName: topicName,
                subscriptionName,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process.</param>
        /// <param name="subscriptionName">The name of the Azure Service Bus Topic subscription to process.</param>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="getConnectionStringFromSecretFunc">The function to look up the connection string scoped to the Azure Service Bus Queue from the secret store.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or <paramref name="getConnectionStringFromSecretFunc"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> or the <paramref name="subscriptionName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(topicName));
            }

            var collection = AddDeprecatedServiceBusTopicMessagePumpWithConnectionString(
                services,
                topicName: topicName,
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
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="getConnectionStringFromConfigurationFunc">The function to look up the connection string scoped to the Azure Service Bus Queue from the configuration.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or <paramref name="getConnectionStringFromConfigurationFunc"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> or <paramref name="subscriptionName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(topicName));
            }

            var collection = AddDeprecatedServiceBusTopicMessagePumpWithConnectionString(
                services,
                topicName: topicName,
                subscriptionName: subscriptionName,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="topicName">THe name of the Azure Service Bus Topic to process.</param>
        /// <param name="subscriptionName">The name of the Azure Service Bus Topic subscription to process.</param>
        /// <param name="serviceBusNamespace">The Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.</param>
        /// <param name="clientId">
        ///     The client ID to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
        ///     <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm" />.
        /// </param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="topicName"/>, the <paramref name="subscriptionName"/>, or the <paramref name="serviceBusNamespace"/> is blank.
        /// </exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(TokenCredential))]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpUsingManagedIdentity(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string serviceBusNamespace,
            string clientId = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(topicName));
            }

            var collection = AddDeprecatedServiceBusTopicMessagePumpWithTokenCredential(
                services,
                topicName: topicName,
                subscriptionName: subscriptionName,
                serviceBusNamespace: serviceBusNamespace,
                tokenCredential: new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId }),
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <remarks>
        ///     <para>When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace.</para>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="subscriptionPrefix">
        ///     The prefix of the Azure Service Bus Topic subscription to process, concat with the <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />.
        /// </param>
        /// <param name="secretName">
        ///     The name of the secret to retrieve the connection string scoped to the Azure Service Bus Topic using your registered <see cref="ISecretProvider" /> implementation.
        /// </param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionPrefix"/> or the <paramref name="secretName"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            string secretName,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Requires a non-blank secret name", nameof(secretName));
            }

            var collection = AddDeprecatedServiceBusTopicMessagePumpWithPrefixWithConnectionString(
                services,
                entityName: null,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: secretProvider => secretProvider.GetRawSecretAsync(secretName),
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <remarks>
        ///     <para>When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace.</para>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="subscriptionPrefix">
        ///     The prefix of the Azure Service Bus Topic subscription to process, concat with the <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />.
        /// </param>
        /// <param name="getConnectionStringFromSecretFunc">The function to look up the connection string scoped to the Azure Service Bus Queue from the secret store.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="getConnectionStringFromSecretFunc"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionPrefix"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddDeprecatedServiceBusTopicMessagePumpWithPrefixWithConnectionString(
                services,
                entityName: null,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <remarks>
        ///     When using this approach; the connection string should be scoped to the topic that is being processed, not the namespace.
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="subscriptionPrefix">
        ///     The prefix of the Azure Service Bus Topic subscription to process, concat with the <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />.
        /// </param>
        /// <param name="getConnectionStringFromConfigurationFunc">The function to look up the connection string scoped to the Azure Service Bus Queue from the configuration.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or the <paramref name="getConnectionStringFromConfigurationFunc"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionPrefix"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddDeprecatedServiceBusTopicMessagePumpWithPrefixWithConnectionString(
                services,
                entityName: null,
                subscriptionPrefix: subscriptionPrefix,
                getConnectionStringFromConfigurationFunc:
                getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process.</param>
        /// <param name="subscriptionPrefix">
        ///     The prefix of the Azure Service Bus Topic subscription to process, concat with the <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />.
        /// </param>
        /// <param name="secretName">
        ///     The name of the secret to retrieve the connection string scoped to the Azure Service Bus Topic using your registered <see cref="ISecretProvider" /> implementation.
        /// </param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="topicName"/>, the <paramref name="subscriptionPrefix"/>, or the <paramref name="services"/> is blank.
        /// </exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            string secretName,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(topicName));
            }

            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Requires a non-blank secret name", nameof(secretName));
            }

            var collection = AddDeprecatedServiceBusTopicMessagePumpWithPrefixWithConnectionString(
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
        /// <remarks>
        ///     Make sure that the application has the Arcus secret store configured correctly.
        ///     For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process.</param>
        /// <param name="subscriptionPrefix">
        ///     The prefix of the Azure Service Bus Topic subscription to process, concat with the <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />.
        /// </param>
        /// <param name="getConnectionStringFromSecretFunc">The function to look up the connection string scoped to the Azure Service Bus Topic from the secret store.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="getConnectionStringFromSecretFunc"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> or the <paramref name="subscriptionPrefix"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(topicName));
            }

            var collection = AddDeprecatedServiceBusTopicMessagePumpWithPrefixWithConnectionString(
                services,
                entityName: topicName,
                subscriptionPrefix,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process.</param>
        /// <param name="subscriptionPrefix">
        ///     The prefix of the Azure Service Bus Topic subscription to process, concat with the <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />.
        /// </param>
        /// <param name="getConnectionStringFromConfigurationFunc">The function to look up the connection string scoped to the Azure Service Bus Topic from the configuration.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or the <paramref name="getConnectionStringFromConfigurationFunc"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> or the <paramref name="subscriptionPrefix"/> is blank.</exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(IServiceProvider) + " to create clients yourself")]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(topicName));
            }

            var collection = AddDeprecatedServiceBusTopicMessagePumpWithPrefixWithConnectionString(
                services,
                entityName: topicName,
                subscriptionPrefix: subscriptionPrefix,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        [Obsolete("Will not use " + nameof(ISecretProvider) + " or " + nameof(IConfiguration) + " directly anymore")]
        private static ServiceBusMessageHandlerCollection AddDeprecatedServiceBusTopicMessagePumpWithPrefixWithConnectionString(
            IServiceCollection services,
            string entityName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
        {
            string subscriptionName = CreateSubscriptionName(subscriptionPrefix);

            ServiceBusMessageHandlerCollection collection = AddDeprecatedServiceBusTopicMessagePumpWithConnectionString(
                services,
                topicName: entityName,
                subscriptionName: subscriptionName,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureTopicMessagePump: configureTopicMessagePump);

            return collection;
        }

        [Obsolete("Will be removed in v3.0")]
        private static ServiceBusMessageHandlerCollection AddDeprecatedServiceBusTopicMessagePumpWithConnectionString(
            IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
        {
            ServiceBusMessageHandlerCollection collection = AddServiceBusMessagePump(services,
                CreateSettings,
                configureQueueMessagePump: null,
                configureTopicMessagePump: configureTopicMessagePump);

            return collection;

            AzureServiceBusMessagePumpSettings CreateSettings(IServiceProvider serviceProvider, AzureServiceBusMessagePumpOptions options)
            {
                return new AzureServiceBusMessagePumpSettings(
                    topicName,
                    subscriptionName,
                    ServiceBusEntityType.Topic,
                    getConnectionStringFromConfigurationFunc,
                    getConnectionStringFromSecretFunc,
                    options,
                    serviceProvider);
            }
        }

        /// <summary>
        /// Adds a message pump to consume messages from Azure Service Bus Topic.
        /// </summary>
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="topicName">The name of the Azure Service Bus Topic to process.</param>
        /// <param name="subscriptionPrefix">
        ///     The prefix of the Azure Service Bus Topic subscription to process, concat with the <see cref="IAzureServiceBusTopicMessagePumpOptions.JobId" />.
        /// </param>
        /// <param name="serviceBusNamespace">The Service Bus namespace to connect to. This is likely to be similar to <c>{yournamespace}.servicebus.windows.net</c>.</param>
        /// <param name="clientId">
        ///     The client ID to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
        ///     <see href="https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm" />.
        /// </param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="topicName"/>, or the <paramref name="subscriptionPrefix"/>, or the <paramref name="serviceBusNamespace"/> is blank.
        /// </exception>
        [Obsolete("Will be removed in v3.0, use the " + nameof(AddServiceBusTopicMessagePump) + " overload instead that takes in a " + nameof(TokenCredential))]
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpUsingManagedIdentityWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            string serviceBusNamespace,
            string clientId = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(topicName));
            }

            TokenCredential tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId });

            ServiceBusMessageHandlerCollection collection = AddDeprecatedServiceBusTopicMessagePumpWithTokenCredential(
                services,
                topicName,
                CreateSubscriptionName(subscriptionPrefix),
                serviceBusNamespace,
                tokenCredential,
                configureMessagePump);

            return collection;
        }

        [Obsolete("Will be removed in v3.0")]
        private static string CreateSubscriptionName(string subscriptionPrefix)
        {
            if (string.IsNullOrWhiteSpace(subscriptionPrefix))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus topic subscription prefix", nameof(subscriptionPrefix));
            }

            var messagePumpOptions = AzureServiceBusMessagePumpOptions.DefaultOptions;
            return $"{subscriptionPrefix}-{messagePumpOptions.JobId}";
        }

        [Obsolete("Will be removed in v3.0")]
        private static ServiceBusMessageHandlerCollection AddDeprecatedServiceBusTopicMessagePumpWithTokenCredential(
            IServiceCollection services,
            string topicName,
            string subscriptionName,
            string serviceBusNamespace,
            TokenCredential tokenCredential,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump)
        {
            ServiceBusMessageHandlerCollection collection = AddServiceBusMessagePump(services,
                CreateSettings,
                configureQueueMessagePump: null,
                configureTopicMessagePump: configureTopicMessagePump);

            return collection;

            AzureServiceBusMessagePumpSettings CreateSettings(IServiceProvider serviceProvider, AzureServiceBusMessagePumpOptions options)
            {
                serviceBusNamespace = SanitizeServiceBusNamespace(serviceBusNamespace);

                return new AzureServiceBusMessagePumpSettings(
                    topicName,
                    subscriptionName,
                    ServiceBusEntityType.Topic,
                    _ => new ServiceBusClient(serviceBusNamespace, tokenCredential),
                    _ => new ServiceBusAdministrationClient(serviceBusNamespace, tokenCredential),
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

        private static Func<IServiceProvider, ServiceBusClient> CreateClientFactory(string fullyQualifiedNamespace, TokenCredential credential)
        {
            return _ => new ServiceBusClient(SanitizeServiceBusNamespace(fullyQualifiedNamespace), credential);
        }

        private static string SanitizeServiceBusNamespace(string serviceBusNamespace)
        {
            if (!serviceBusNamespace.EndsWith(".servicebus.windows.net"))
            {
                serviceBusNamespace += ".servicebus.windows.net";
            }

            return serviceBusNamespace;
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
                configureTopicMessagePump: ConvertTopicOptions(configureMessagePump));

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

        [Obsolete("Will be removed in v3.0")]
        private static Action<IAzureServiceBusTopicMessagePumpOptions> ConvertTopicOptions(Action<AzureServiceBusMessagePumpOptions> configureOptions)
        {
            return deprecatedOptions => configureOptions?.Invoke((AzureServiceBusMessagePumpOptions) deprecatedOptions);
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusMessagePump(
            IServiceCollection services,
            Func<IServiceProvider, AzureServiceBusMessagePumpOptions, AzureServiceBusMessagePumpSettings> createSettings,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            AzureServiceBusMessagePumpOptions options =
                DetermineMessagePumpOptions(configureQueueMessagePump, configureTopicMessagePump);

            ServiceBusMessageHandlerCollection collection = services.AddServiceBusMessageRouting(provider =>
            {
                var logger = provider.GetService<ILogger<AzureServiceBusMessageRouter>>();
                return new AzureServiceBusMessageRouter(provider, options.Routing, logger);
            });
            collection.JobId = options.JobId;

            services.TryAddSingleton<IMessagePumpLifetime, DefaultMessagePumpLifetime>();
            services.TryAddSingleton<IMessagePumpCircuitBreaker>(provider => new DefaultMessagePumpCircuitBreaker(provider, provider.GetService<ILogger<DefaultMessagePumpCircuitBreaker>>()));

            services.AddHostedService(provider =>
            {
                var router = provider.GetService<IAzureServiceBusMessageRouter>();
                var logger = provider.GetService<ILogger<AzureServiceBusMessagePump>>();

                AzureServiceBusMessagePumpSettings settings = createSettings(provider, options);
                return new AzureServiceBusMessagePump(settings, provider, router, logger);
            });

            collection.WithServiceBusSerilogRequestTracking(opt =>
            {
                opt.Enricher.CycleIdPropertyName = options.Routing.CorrelationEnricher.CycleIdPropertyName;
                opt.Enricher.OperationIdPropertyName = options.Routing.CorrelationEnricher.OperationIdPropertyName;
                opt.Enricher.TransactionIdPropertyName = options.Routing.CorrelationEnricher.TransactionIdPropertyName;
                opt.Enricher.OperationParentIdPropertyName = options.Routing.CorrelationEnricher.OperationParentIdPropertyName;
            });

            return collection;
        }

        [Obsolete("Will be removed in v3.0")]
        private static AzureServiceBusMessagePumpOptions DetermineMessagePumpOptions(
            Action<IAzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump)
        {
            var options = AzureServiceBusMessagePumpOptions.DefaultOptions;
            configureQueueMessagePump?.Invoke(options);
            configureTopicMessagePump?.Invoke(options);

            return options;
        }
    }
}