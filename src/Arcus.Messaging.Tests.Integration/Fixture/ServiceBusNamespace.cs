using GuardNet;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents the configuration values related to an Azure Service Bus instance.
    /// </summary>
    public class ServiceBusNamespace
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusNamespace" /> class.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant on which the Azure instance is run.</param>
        /// <param name="azureSubscriptionId">The ID of the Azure subscription.</param>
        /// <param name="resourceGroup">The name of the resource group where the Azure Service Bus is located.</param>
        /// <param name="namespace">The namespace in which the Azure Service Bus is categorized.</param>
        /// <param name="queueName">The name of the Queue in the Azure Service Bus instance.</param>
        /// <param name="topicName">The name of the Topic in the Azure Service Bus instance.</param>
        /// <param name="authorizationRuleName">The name of the authorization rule that describes the available permissions.</param>
        public ServiceBusNamespace(
            string tenantId,
            string azureSubscriptionId,
            string resourceGroup,
            string @namespace,
            string queueName,
            string topicName,
            string authorizationRuleName)
        {
            Guard.NotNullOrWhitespace(azureSubscriptionId, nameof(azureSubscriptionId));
            Guard.NotNullOrWhitespace(tenantId, nameof(azureSubscriptionId));
            Guard.NotNullOrWhitespace(resourceGroup, nameof(resourceGroup));
            Guard.NotNullOrWhitespace(@namespace, nameof(@namespace));
            Guard.NotNullOrWhitespace(queueName, nameof(queueName));
            Guard.NotNullOrWhitespace(topicName, nameof(topicName));
            Guard.NotNullOrWhitespace(authorizationRuleName, nameof(authorizationRuleName));

            SubscriptionId = azureSubscriptionId;
            TenantId = tenantId;
            ResourceGroup = resourceGroup;
            Namespace = @namespace;
            QueueName = queueName;
            TopicName = topicName;
            AuthorizationRuleName = authorizationRuleName;
        }

        /// <summary>
        /// Gets the ID of the tenant on which the Azure instance is run.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Gets the ID of the Azure subscription.
        /// </summary>
        public string SubscriptionId { get; }

        /// <summary>
        /// Gets the name of the resource group where the Azure Service Bus is located.
        /// </summary>
        public string ResourceGroup { get; }

        /// <summary>
        /// Gets the namespace in which the Azure Service Bus is categorized.
        /// </summary>
        public string Namespace { get; }
        
        /// <summary>
        /// Gets the name of the Queue in the Azure Service Bus namespace.
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Gets the name of the Topic in the Azure Service Bus namespace.
        /// </summary>
        public string TopicName { get; }

        /// <summary>
        /// Gets the name of the authorization rule that describes the available permissions.
        /// </summary>
        public string AuthorizationRuleName { get; }
    }
}
