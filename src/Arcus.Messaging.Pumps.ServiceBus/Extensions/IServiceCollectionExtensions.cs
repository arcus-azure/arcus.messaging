using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.Configuration;
using Arcus.Security.Core;
using Azure.Core;
using Azure.Identity;
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
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusQueueMessagePump(
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
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
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
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string secretName,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Requires a non-blank secret name", nameof(secretName));
            }

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
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            string secretName,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Requires a non-blank secret name", nameof(secretName));
            }

            var collection = AddServiceBusQueueMessagePump(
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
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusQueueMessagePump(
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
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            this IServiceCollection services,
            string queueName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusQueueMessagePump(
                services,
                entityName: queueName,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                configureQueueMessagePump: configureMessagePump);

            return collection;
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
        public static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePumpUsingManagedIdentity(
            this IServiceCollection services,
            string queueName,
            string serviceBusNamespace,
            string clientId = null,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusQueueMessagePump(
                services,
                entityName: queueName,
                fullyQualifiedNamespace: serviceBusNamespace,
                tokenCredential: new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId }),
                configureQueueMessagePump: configureMessagePump);

            return collection;
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusQueueMessagePump(
            IServiceCollection services,
            string entityName,
            string fullyQualifiedNamespace = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            TokenCredential tokenCredential = null,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump = null)
        {
            var collection = AddServiceBusMessagePump(
                services,
                entityName,
                subscriptionName: null,
                ServiceBusEntityType.Queue,
                serviceBusNamespace: fullyQualifiedNamespace,
                configureQueueMessagePump: configureQueueMessagePump,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                tokenCredential: tokenCredential);

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
        /// <param name="secretName">
        ///     The name of the secret to retrieve the connection string scoped to the Azure Service Bus Topic using your registered <see cref="ISecretProvider" /> implementation.
        /// </param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionName"/> or <paramref name="secretName"/> is blank.</exception>
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

            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: null,
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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: null,
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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string secretName,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of services to add the Azure Service Bus Topic message pump");
            Guard.NotNullOrWhitespace(topicName, nameof(topicName), "Requires a non-blank Azure Service Bus Topic name");
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName), "Requires a non-blank Azure Service Bus Topic subscription name");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank secret name to look up the connection string to authenticate with the Azure Service Bus Topic");

            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: topicName,
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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
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
        /// <param name="services">The collection of services to add the message pump to.</param>
        /// <param name="getConnectionStringFromConfigurationFunc">The function to look up the connection string scoped to the Azure Service Bus Queue from the configuration.</param>
        /// <param name="configureMessagePump">The capability to configure additional options on how the message pump should behave.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="services"/> or <paramref name="getConnectionStringFromConfigurationFunc"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicName"/> or <paramref name="subscriptionName"/> is blank.</exception>
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: topicName,
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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpUsingManagedIdentity(
            this IServiceCollection services,
            string topicName,
            string subscriptionName,
            string serviceBusNamespace,
            string clientId = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusTopicMessagePump(
                services,
                entityName: topicName,
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

            var collection = AddServiceBusTopicMessagePumpWithPrefix(
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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusTopicMessagePumpWithPrefix(
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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            string secretName,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Requires a non-blank secret name", nameof(secretName));
            }

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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusTopicMessagePumpWithPrefix(
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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: topicName,
                subscriptionPrefix: subscriptionPrefix,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
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
        public static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpUsingManagedIdentityWithPrefix(
            this IServiceCollection services,
            string topicName,
            string subscriptionPrefix,
            string serviceBusNamespace,
            string clientId = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureMessagePump = null)
        {
            var collection = AddServiceBusTopicMessagePumpWithPrefix(
                services,
                entityName: topicName,
                subscriptionPrefix: subscriptionPrefix,
                serviceBusNamespace: serviceBusNamespace,
                tokenCredential: new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId }),
                configureTopicMessagePump: configureMessagePump);

            return collection;
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePumpWithPrefix(
            IServiceCollection services,
            string entityName,
            string subscriptionPrefix,
            string serviceBusNamespace = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            TokenCredential tokenCredential = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
        {
            if (string.IsNullOrWhiteSpace(subscriptionPrefix))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus topic subscription prefix", nameof(subscriptionPrefix));
            }

            var messagePumpOptions = AzureServiceBusMessagePumpOptions.DefaultTopicOptions;
            string subscriptionName = $"{subscriptionPrefix}-{messagePumpOptions.JobId}";

            ServiceBusMessageHandlerCollection collection = AddServiceBusTopicMessagePump(
                services,
                entityName: entityName,
                subscriptionName: subscriptionName,
                serviceBusNamespace: serviceBusNamespace,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                tokenCredential: tokenCredential,
                configureTopicMessagePump: configureTopicMessagePump);

            return collection;
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            IServiceCollection services,
            string entityName,
            string subscriptionName = null,
            string serviceBusNamespace = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            TokenCredential tokenCredential = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null)
        {
            ServiceBusMessageHandlerCollection collection = AddServiceBusMessagePump(
                services,
                entityName,
                subscriptionName,
                ServiceBusEntityType.Topic,
                serviceBusNamespace: serviceBusNamespace,
                configureTopicMessagePump: configureTopicMessagePump,
                getConnectionStringFromConfigurationFunc: getConnectionStringFromConfigurationFunc,
                getConnectionStringFromSecretFunc: getConnectionStringFromSecretFunc,
                tokenCredential: tokenCredential);

            return collection;
        }

        private static ServiceBusMessageHandlerCollection AddServiceBusMessagePump(
            IServiceCollection services,
            string entityName,
            string subscriptionName,
            ServiceBusEntityType serviceBusEntity,
            string serviceBusNamespace = null,
            Action<IAzureServiceBusQueueMessagePumpOptions> configureQueueMessagePump = null,
            Action<IAzureServiceBusTopicMessagePumpOptions> configureTopicMessagePump = null,
            Func<IConfiguration, string> getConnectionStringFromConfigurationFunc = null,
            Func<ISecretProvider, Task<string>> getConnectionStringFromSecretFunc = null,
            TokenCredential tokenCredential = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(entityName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(entityName));
            }

            if (serviceBusEntity is ServiceBusEntityType.Topic && string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus topic subscription name", nameof(subscriptionName));
            }

            AzureServiceBusMessagePumpOptions options =
                DetermineAzureServiceBusMessagePumpOptions(serviceBusEntity, configureQueueMessagePump, configureTopicMessagePump);

            ServiceBusMessageHandlerCollection collection = services.AddServiceBusMessageRouting(provider =>
            {
                var logger = provider.GetService<ILogger<AzureServiceBusMessageRouter>>();
                return new AzureServiceBusMessageRouter(provider, options.Routing, logger);
            });
            collection.JobId = options.JobId;

            services.AddMessagePump(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<AzureServiceBusMessagePump>>();
                if (subscriptionName != null && subscriptionName.Length > 50)
                {
                    logger.LogWarning("Azure Service Bus Topic subscription name was truncated to 50 characters");
                    subscriptionName = subscriptionName.Substring(0, 50);
                }

                AzureServiceBusMessagePumpSettings settings;
                if (tokenCredential is null)
                {
                    settings = new AzureServiceBusMessagePumpSettings(
                        entityName, subscriptionName, serviceBusEntity, getConnectionStringFromConfigurationFunc, getConnectionStringFromSecretFunc, options, serviceProvider);
                }
                else
                {
                    settings = new AzureServiceBusMessagePumpSettings(
                        entityName, subscriptionName, serviceBusEntity, serviceBusNamespace, tokenCredential, options, serviceProvider);
                }

                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var router = serviceProvider.GetService<IAzureServiceBusMessageRouter>();
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