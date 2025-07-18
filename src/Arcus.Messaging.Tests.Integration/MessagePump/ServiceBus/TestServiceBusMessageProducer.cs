using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Integration.Fixture;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus
{
    /// <summary>
    /// Represents an event producer which sends events to an Azure Service Bus.
    /// </summary>
    public class TestServiceBusMessageProducer
    {
        private readonly string _entityName;
        private readonly ServiceBusConfig _config;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceBusMessageProducer" /> class.
        /// </summary>
        public TestServiceBusMessageProducer(string entityName, ServiceBusConfig config, ILogger logger)
        {
            _entityName = entityName;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
        }

        /// <summary>
        /// Creates an <see cref="TestServiceBusMessageProducer"/> instance which sends events to an Azure Service Bus.
        /// </summary>
        public static TestServiceBusMessageProducer CreateFor(string entityName, ServiceBusConfig configuration, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            return new TestServiceBusMessageProducer(entityName, configuration, logger);
        }

        /// <summary>
        /// Sends the <paramref name="messages"/> to the configured Azure Service Bus.
        /// </summary>
        /// <param name="messages">The message to send.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messages"/> is <c>null</c>.</exception>
        public async Task ProduceAsync(params ServiceBusMessage[] messages)
        {
            ArgumentNullException.ThrowIfNull(messages);

            await using var client = new ServiceBusClient(_config.HostName, _config.ServicePrincipal.GetCredential());
            await using ServiceBusSender messageSender = client.CreateSender(_entityName);

            foreach (var message in messages)
            {
                string diagnosticId = message.ApplicationProperties.TryGetValue("Diagnostic-Id", out object value) ? value?.ToString() : null;
                _logger.LogTrace("[Test] Send Azure Service Bus message '{MessageId}' [Encoding={Encoding}, Diagnostic-Id={DiagnosticId}]", message.MessageId, message.ApplicationProperties[PropertyNames.Encoding], diagnosticId);
            }

            await messageSender.SendMessagesAsync(messages);
        }
    }
}