using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Workers.ServiceBus.Fixture;
using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using TestConfig = Arcus.Messaging.Tests.Integration.Fixture.TestConfig;

namespace Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus
{
    /// <summary>
    /// Represents a service that consumes Azure Service Bus messages from the dead-letter sub-queue.
    /// </summary>
    public class TestServiceMessageConsumer
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        private TestServiceMessageConsumer(string connectionString, ILogger logger)
        {
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString), "Requires an Azure Service Bus connection string so dead-lettered messages can be consumed");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write informational messages during the dead-lettered message consuming from an Azure Service Bus queue");

            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// Creates an <see cref="TestServiceMessageConsumer"/> instance that consumes dead-lettered messages from an Azure Service Bus queue.
        /// </summary>
        /// <param name="config">The integration test configuration where the connection string to the Azure Service Bus queue is present.</param>
        /// <param name="logger">The logger instance to write informational messages during the message consuming.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> or the <paramref name="logger"/> is <c>null</c>.</exception>
        public static TestServiceMessageConsumer CreateForQueue(TestConfig config, ILogger logger)
        {
            Guard.NotNull(config, nameof(config), "Requires an integration test configuration to retrieve the Azure Service Bus queue connection string so dead-lettered messages can be consumed");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write informational messages during the dead-lettered message consuming from an Azure Service Bus queue");

            string connectionString = config.GetServiceBusQueueConnectionString();
            return new TestServiceMessageConsumer(connectionString, logger);
        }

        /// <summary>
        /// Verifies that the message is completed on the Azure Service Bus queue.
        /// </summary>
        public async Task AssertCompletedMessageAsync(string messageId)
        {
            var properties = ServiceBusConnectionStringProperties.Parse(_connectionString);
            await using var client = new ServiceBusClient(_connectionString);
            await using var receiver = client.CreateReceiver(properties.EntityPath);

            IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.ReceiveMessagesAsync(1, maxWaitTime: TimeSpan.FromSeconds(2));
            Assert.DoesNotContain(messages, msg => msg.MessageId == messageId);
        }

        /// <summary>
        /// Tries receiving a single abandoned message on the Azure Service Bus queue.
        /// </summary>
        /// <exception cref="TimeoutException">Thrown when no abandoned messages can be consumed within the configured time-out.</exception>
        public async Task AssertAbandonMessageAsync(string messageId)
        {
            var properties = ServiceBusConnectionStringProperties.Parse(_connectionString);
            await using var client = new ServiceBusClient(_connectionString);
            await using var receiver = client.CreateReceiver(properties.EntityPath);

            ServiceBusReceivedMessage message =
                await Poll.Target(() => receiver.ReceiveMessageAsync())
                          .Until(msg => msg != null && msg.MessageId == messageId && msg.DeliveryCount > 1)
                          .Every(TimeSpan.FromSeconds(1))
                          .Timeout(TimeSpan.FromMinutes(2))
                          .FailWith($"cannot receive abandoned message with the message ID: '{messageId}' in time");

            await receiver.CompleteMessageAsync(message);
        }

        /// <summary>
        /// Tries receiving a single dead lettered message on the Azure Service Bus dead letter queue.
        /// </summary>
        /// <exception cref="TimeoutException">Thrown when no dead-lettered messages can be consumed within the configured time-out.</exception>
        public async Task AssertDeadLetterMessageAsync(string messageId)
        {
            var properties = ServiceBusConnectionStringProperties.Parse(_connectionString);
            var options = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter };


            async Task<ServiceBusReceivedMessage> ReceiveMessageAsync()
            {
                await using var client = new ServiceBusClient(_connectionString);
                await using var receiver = client.CreateReceiver(properties.EntityPath, options);
                
                ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
                if (message != null)
                {
                    await receiver.CompleteMessageAsync(message);
                }

                return message;
            }

            var message = 
                await Poll.Target(ReceiveMessageAsync)
                          .Until(message =>
                          {
                              if (message is null)
                              {
                                  return false;
                              }

                              try
                              {
                                  string json = message.Body.ToString();
                                  var order = JsonConvert.DeserializeObject<OrderCreatedEventData>(json, new MessageCorrelationInfoJsonConverter());

                                  return order.Id == messageId;
                              }
                              catch (JsonException)
                              {
                                  return false;
                              }
                          })
                          .Every(TimeSpan.FromSeconds(1))
                          .Timeout(TimeSpan.FromMinutes(2))
                          .FailWith($"cannot receive dead-lettered message with message ID: '{messageId}' in time");
        }
    }
}
