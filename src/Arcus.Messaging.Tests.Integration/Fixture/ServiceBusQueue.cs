using System;
using System.Collections.Generic;
using System.Text;
using GuardNet;

namespace Arcus.Messaging.Tests.Integration.Fixture
{
    public class ServiceBusQueue
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public ServiceBusQueue(string subscriptionId, string tenantId, string resourceGroup, string @namespace, string queueName, string authorizationRuleName)
        {
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId));
            Guard.NotNullOrWhitespace(tenantId, nameof(subscriptionId));
            Guard.NotNullOrWhitespace(resourceGroup, nameof(resourceGroup));
            Guard.NotNullOrWhitespace(@namespace, nameof(@namespace));
            Guard.NotNullOrWhitespace(queueName, nameof(queueName));
            Guard.NotNullOrWhitespace(authorizationRuleName, nameof(authorizationRuleName));

            SubscriptionId = subscriptionId;
            TenantId = tenantId;
            ResourceGroup = resourceGroup;
            Namespace = @namespace;
            QueueName = queueName;
            AuthorizationRuleName = authorizationRuleName;
        }

        public string SubscriptionId { get; }

        public string TenantId { get; }

        public string ResourceGroup { get; }

        public string Namespace { get; }

        public string QueueName { get; }

        public string AuthorizationRuleName { get; }
    }
}
