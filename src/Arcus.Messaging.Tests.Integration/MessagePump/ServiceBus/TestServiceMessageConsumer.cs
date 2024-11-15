using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Workers.ServiceBus.Fixture;
using Arcus.Testing;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Sdk;

namespace Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus
{
    /// <summary>
    /// Represents a service that consumes Azure Service Bus messages from the dead-letter sub-queue.
    /// </summary>
    public class TestServiceMessageConsumer
    {
        private readonly string _entityName;
        private readonly ServiceBusConfig _config;
        private readonly ILogger _logger;

        private TestServiceMessageConsumer(string entityName, ServiceBusConfig config, ILogger logger)
        {
            _entityName = entityName;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Creates an <see cref="TestServiceMessageConsumer"/> instance that consumes dead-lettered messages from an Azure Service Bus queue.
        /// </summary>
        public static TestServiceMessageConsumer CreateForQueue(string entityName, ServiceBusConfig config, ILogger logger)
        {
            return new TestServiceMessageConsumer(entityName, config, logger);
        }

        /// <summary>
        /// Verifies that the message is completed on the Azure Service Bus queue.
        /// </summary>
        public async Task AssertCompletedMessageAsync(string messageId)
        {
            await Poll.UntilAvailableAsync<XunitException>(async () =>
            {
                await using ServiceBusClient client = _config.GetClient();
                await using ServiceBusReceiver receiver = client.CreateReceiver(_entityName);

                IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.PeekMessagesAsync(100);
                Assert.DoesNotContain(messages, msg => msg.MessageId == messageId);

            }, options => options.FailureMessage = $"Azure Service bus message '{messageId}' did not get completed in time");
        }

        /// <summary>
        /// Tries receiving a single abandoned message on the Azure Service Bus queue.
        /// </summary>
        /// <exception cref="TimeoutException">Thrown when no abandoned messages can be consumed within the configured time-out.</exception>
        public async Task AssertAbandonMessageAsync(string messageId, bool completeUponReceive = false)
        {
            await using ServiceBusClient client = _config.GetClient();
            await using ServiceBusReceiver receiver = client.CreateReceiver(_entityName);

            ServiceBusReceivedMessage message = 
                await Poll.Target(() => receiver.ReceiveMessageAsync())
                          .Until(msg => msg != null && msg.MessageId == messageId && msg.DeliveryCount > 1)
                          .Every(TimeSpan.FromSeconds(1))
                          .Timeout(TimeSpan.FromMinutes(2))
                          .FailWith($"cannot receive abandoned message with the message ID: '{messageId}' in time");

            if (completeUponReceive)
            {
                await receiver.CompleteMessageAsync(message);
            }
        }

        /// <summary>
        /// Tries receiving a single dead lettered message on the Azure Service Bus dead letter queue.
        /// </summary>
        /// <exception cref="TimeoutException">Thrown when no dead-lettered messages can be consumed within the configured time-out.</exception>
        public async Task AssertDeadLetterMessageAsync(string messageId, TimeSpan? timeout = null)
        {
            var options = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter };

            async Task<ServiceBusReceivedMessage> ReceiveMessageAsync()
            {
                await using ServiceBusClient client = _config.GetClient();
                await using ServiceBusReceiver receiver = client.CreateReceiver(_entityName, options);
                
                _logger.LogTrace("Start looking for dead-lettered message '{MessageId}' for Azure Service bus queue '{QueueName}'", messageId, _entityName);
                ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
                if (message != null)
                {
                    _logger.LogTrace("Found dead-lettered message '{MessageId}' for Azure Service bus queue '{QueueName}'", messageId, _entityName);
                    await receiver.CompleteMessageAsync(message);
                }

                return message;
            }

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
                      .Timeout(timeout ?? TimeSpan.FromMinutes(2))
                      .FailWith($"cannot receive dead-lettered message with message ID: '{messageId}' in time");
        }
    }
}
