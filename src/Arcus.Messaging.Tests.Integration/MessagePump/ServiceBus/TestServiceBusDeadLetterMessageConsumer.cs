using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

namespace Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus
{
    /// <summary>
    /// Represents a service that consumes Azure Service Bus messages from the dead-letter sub-queue.
    /// </summary>
    public class TestServiceBusDeadLetterMessageConsumer
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        private TestServiceBusDeadLetterMessageConsumer(string connectionString, ILogger logger)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString), "Requires an Azure Service Bus connection string so dead-lettered messages can be consumed");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write informational messages during the dead-lettered message consuming from an Azure Service Bus queue");

            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// Creates an <see cref="TestServiceBusDeadLetterMessageConsumer"/> instance that consumes dead-lettered messages from an Azure Service Bus queue.
        /// </summary>
        /// <param name="config">The integration test configuration where the connection string to the Azure Service Bus queue is present.</param>
        /// <param name="logger">The logger instance to write informational messages during the message consuming.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> or the <paramref name="logger"/> is <c>null</c>.</exception>
        public static TestServiceBusDeadLetterMessageConsumer CreateForQueue(TestConfig config, ILogger logger)
        {
            Guard.NotNull(config, nameof(config), "Requires an integration test configuration to retrieve the Azure Service Bus queue connection string so dead-lettered messages can be consumed");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write informational messages during the dead-lettered message consuming from an Azure Service Bus queue");

            string connectionString = config.GetServiceBusQueueConnectionString();
            return new TestServiceBusDeadLetterMessageConsumer(connectionString, logger);
        }

        /// <summary>
        /// Tries receiving a single dead lettered message on the Azure Service Bus dead letter queue.
        /// </summary>
        /// <exception cref="TimeoutRejectedException">Thrown when no dead-lettered messages can be consumed within the configured time-out.</exception>
        public async Task AssertDeadLetterMessageAsync()
        {
            var properties = ServiceBusConnectionStringProperties.Parse(_connectionString);
            var options = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter };

            await using (var client = new ServiceBusClient(_connectionString))
            await using (var receiver = client.CreateReceiver(properties.EntityPath, options))
            {
                var retryPolicy = Policy.HandleResult<ServiceBusReceivedMessage>(result => result is null)
                                        .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(1));

                await Policy.TimeoutAsync(TimeSpan.FromMinutes(2))
                            .WrapAsync(retryPolicy)
                            .ExecuteAsync(async () =>
                            {
                                ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
                                if (message != null)
                                {
                                    _logger.LogInformation("Received dead lettered message in test suite");
                                    await receiver.CompleteMessageAsync(message);
                                }
                                else
                                {
                                    _logger.LogInformation("No dead lettered message received in test suite, retrying...");
                                }

                                return message;
                            });
            }
        }
    }
}
