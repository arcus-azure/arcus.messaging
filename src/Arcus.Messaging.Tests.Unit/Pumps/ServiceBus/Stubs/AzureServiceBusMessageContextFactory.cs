using System;
using System.Collections.Generic;
using System.Reflection;
using Arcus.Messaging.Pumps.ServiceBus;
using Microsoft.Azure.ServiceBus;

namespace Arcus.Messaging.Tests.Unit.Pumps.ServiceBus.Stubs
{
    /// <summary>
    /// Test factory to generate valid <see cref="AzureServiceBusMessageContext"/> instances.
    /// </summary>
    public static class AzureServiceBusMessageContextFactory
    {
        /// <summary>
        /// Generates a valid <see cref="AzureServiceBusMessageContext"/> instance.
        /// </summary>
        public static AzureServiceBusMessageContext Generate()
        {
            var systemProperties = new Message.SystemPropertiesCollection();
            systemProperties.GetType()
                .GetProperty(nameof(systemProperties.SequenceNumber))
                ?.SetValue(systemProperties, 1, BindingFlags.Instance | BindingFlags.NonPublic, null, null, null);

            var context = new AzureServiceBusMessageContext(
                $"message-id-{Guid.NewGuid()}", 
                systemProperties, 
                new Dictionary<string, object>());

            return context;
        }
    }
}
