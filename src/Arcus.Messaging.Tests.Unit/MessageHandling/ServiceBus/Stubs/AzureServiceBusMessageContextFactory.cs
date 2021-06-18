using System;
using System.Collections.Generic;
using System.Reflection;
using Arcus.Messaging.Abstractions.ServiceBus;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Bogus;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus.Stubs
{
    /// <summary>
    /// Test factory to generate valid <see cref="AzureServiceBusMessageContext"/> instances.
    /// </summary>
    public static class AzureServiceBusMessageContextFactory
    {
        private static readonly Faker BogusGenerator = new Faker();
        
        /// <summary>
        /// Generates a valid <see cref="AzureServiceBusMessageContext"/> instance.
        /// </summary>
        public static AzureServiceBusMessageContext Generate()
        {
            var amqp = new AmqpAnnotatedMessage(new AmqpMessageBody(new ReadOnlyMemory<byte>[0]));
            amqp.Header.DeliveryCount = BogusGenerator.Random.UInt();
            
            var message = (ServiceBusReceivedMessage) Activator.CreateInstance(typeof(ServiceBusReceivedMessage),
                BindingFlags.NonPublic | BindingFlags.Instance,
                args: new object[] { amqp },
                binder: null,
                culture: null,
                activationAttributes: null);
            
            AzureServiceBusSystemProperties systemProperties = message.GetSystemProperties();
            
            var context = new AzureServiceBusMessageContext(
                $"message-id-{Guid.NewGuid()}", 
                $"job-id-{Guid.NewGuid()}",
                systemProperties, 
                new Dictionary<string, object>());

            return context;
        }
    }
}
