﻿using System;
using GuardNet;

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
            ServiceBusEntity entity, 
            string entityName,
            string authorizationRuleName)
        {
            Guard.NotNullOrWhitespace(resourceGroup, nameof(resourceGroup));
            Guard.NotNullOrWhitespace(@namespace, nameof(@namespace));
            Guard.NotNullOrWhitespace(entityName, nameof(entityName));
            Guard.NotNullOrWhitespace(authorizationRuleName, nameof(authorizationRuleName));
            Guard.For<ArgumentException>(
                () => !Enum.IsDefined(typeof(ServiceBusEntity), entity), 
                $"Azure Service Bus entity '{entity}' is not defined in the '{nameof(ServiceBusEntity)}' enumeration");

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
        public ServiceBusEntity Entity { get; }
        
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