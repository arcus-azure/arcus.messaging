using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus
{
    /// <summary>
    /// Represents an event producer which sends events to an Azure Service Bus.
    /// </summary>
    public class TestServiceBusMessageProducer
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceBusMessageProducer"/> class.
        /// </summary>
        /// <param name="connectionString">The Azure Service Bus entity-scoped connection string to send messages to.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString"/> is blank.</exception>
        public TestServiceBusMessageProducer(string connectionString)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString), "Requires a non-blank Azure Service Bus entity-scoped connection string");
            _connectionString = connectionString;
        }

        /// <summary>
        /// Creates an <see cref="TestServiceBusMessageProducer"/> instance which sends events to an Azure Service Bus topic subscription.
        /// </summary>
        /// <param name="configuration">The test configuration used in this test suite.</param>
        public static TestServiceBusMessageProducer CreateForTopic(TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the Azure Service Bus topic entity-scoped connection string");
            return CreateFor(configuration, ServiceBusEntityType.Topic);
        }

        /// <summary>
        /// Creates an <see cref="TestServiceBusMessageProducer"/> instance which sends events to an Azure Service Bus queue.
        /// </summary>
        /// <param name="configuration">The test configuration used in this test suite.</param>
        public static TestServiceBusMessageProducer CreateForQueue(TestConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the Azure Service Bus queue entity-scoped connection string");
            return CreateFor(configuration, ServiceBusEntityType.Queue);
        }

        /// <summary>
        /// Creates an <see cref="TestServiceBusMessageProducer"/> instance which sends events to an Azure Service Bus.
        /// </summary>
        /// <param name="configuration">The test configuration used in this test suite.</param>
        /// <param name="entityType"></param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static TestServiceBusMessageProducer CreateFor(TestConfig configuration, ServiceBusEntityType entityType)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration to retrieve the Azure Service Bus entity-scoped connection string");

            string connectionString = configuration.GetServiceBusConnectionString(entityType);
            return new TestServiceBusMessageProducer(connectionString);
        }

        /// <summary>
        /// Sends the <paramref name="message"/> to the configured Azure Service Bus.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="message"/> is <c>null</c>.</exception>
        public async Task ProduceAsync(ServiceBusMessage message)
        {
            Guard.NotNull(message, nameof(message), "Requires an Azure Service Bus message to send");

            var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(_connectionString);
            await using (var client = new ServiceBusClient(_connectionString))
            {
                ServiceBusSender messageSender = client.CreateSender(connectionStringProperties.EntityPath);

                try
                {
                    await messageSender.SendMessageAsync(message);
                }
                finally
                {
                    await messageSender.CloseAsync();
                }
            }
        }
    }
}