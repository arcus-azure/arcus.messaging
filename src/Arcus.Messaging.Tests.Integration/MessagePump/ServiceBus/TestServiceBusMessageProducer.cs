using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Testing;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus
{
    /// <summary>
    /// Represents an event producer which sends events to an Azure Service Bus.
    /// </summary>
    public class TestServiceBusMessageProducer
    {
        private readonly string _connectionString;
        private readonly string _entityName;
        private readonly ServiceBusConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceBusMessageProducer" /> class.
        /// </summary>
        public TestServiceBusMessageProducer(string entityName, ServiceBusConfig config)
        {
            _entityName = entityName;
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceBusMessageProducer" /> class.
        /// </summary>
        public TestServiceBusMessageProducer(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Creates an <see cref="TestServiceBusMessageProducer"/> instance which sends events to an Azure Service Bus.
        /// </summary>
        public static TestServiceBusMessageProducer CreateFor(string entityName, TestConfig configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            return new TestServiceBusMessageProducer(entityName, configuration.GetServiceBus());
        }

        /// <summary>
        /// Sends the <paramref name="messages"/> to the configured Azure Service Bus.
        /// </summary>
        /// <param name="messages">The message to send.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messages"/> is <c>null</c>.</exception>
        public async Task ProduceAsync(params ServiceBusMessage[] messages)
        {
            ArgumentNullException.ThrowIfNull(messages);

            await using var client = _connectionString is null
                ? new ServiceBusClient(_config.HostName, _config.ServicePrincipal.GetCredential())
                : new ServiceBusClient(_connectionString);
            
            await using ServiceBusSender messageSender = client.CreateSender(_entityName);
            await messageSender.SendMessagesAsync(messages);
        }
    }
}