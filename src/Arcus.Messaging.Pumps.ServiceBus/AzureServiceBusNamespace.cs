using System;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents the namespace of a Azure Service Bus resource; where the Azure Service Bus is located.
    /// </summary>
    public class AzureServiceBusNamespace
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusNamespace"/> class.
        /// </summary>
        /// <param name="resourceGroup">The resource group where the Azure Service Bus resource is located.</param>
        /// <param name="namespace">The namespace where the Azure Service Bus is categorized.</param>
        /// <param name="entity">The entity type of the Azure Service Bus resource.</param>
        /// <param name="entityName">The entity name of the Azure Service Bus resource.</param>
        /// <param name="authorizationRuleName">The name of the authorization rule to use when authorizing with the Azure Service Bus.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="resourceGroup"/>, <paramref name="namespace"/>, <paramref name="entityName"/>, or <paramref name="authorizationRuleName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="entity"/> is not defined within the bounds of the enumeration.</exception>
        public AzureServiceBusNamespace(
            string resourceGroup,
            string @namespace,
            ServiceBusEntityType entity,
            string entityName,
            string authorizationRuleName)
        {
            if (string.IsNullOrWhiteSpace(resourceGroup))
            {
                throw new ArgumentException("Requires a non-blank resource group name", nameof(resourceGroup));
            }

            if (string.IsNullOrWhiteSpace(@namespace))
            {
                throw new ArgumentException("Requires a non-blank namespace", nameof(@namespace));
            }

            if (string.IsNullOrWhiteSpace(entityName))
            {
                throw new ArgumentException("Requires a non-blank Azure Service bus entity name", nameof(entityName));
            }

            if (string.IsNullOrWhiteSpace(authorizationRuleName))
            {
                throw new ArgumentException("Requires a non-blank authorization rule name", nameof(authorizationRuleName));
            }

            if (!Enum.IsDefined(entity) || entity is ServiceBusEntityType.Unknown)
            {
                throw new ArgumentException($"Requires either a '{ServiceBusEntityType.Topic}' or '{ServiceBusEntityType.Queue}' as Azure Service bus entity type", nameof(entity));
            }

            ResourceGroup = resourceGroup;
            Namespace = @namespace;
            Entity = entity;
            EntityName = entityName;
            AuthorizationRuleName = authorizationRuleName;
        }

        /// <summary>
        /// Gets the resource group where the Azure Service Bus resource is located.
        /// </summary>
        public string ResourceGroup { get; }

        /// <summary>
        /// Gets the namespace where the Azure Service Bus is categorized.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Gets the entity type of the Azure Service Bus resource.
        /// </summary>
        public ServiceBusEntityType Entity { get; }

        /// <summary>
        /// Gets the entity name of the Azure Service Bus resource.
        /// </summary>
        public string EntityName { get; }

        /// <summary>
        /// Gets the name of the authorization rule to use when authorizing with the Azure Service Bus.
        /// </summary>
        public string AuthorizationRuleName { get; }
    }
}