using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Arcus.Testing;
using Azure;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;
using static Microsoft.Extensions.Logging.ServiceBusEntityType;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetter_PublishServiceBusMessage_MessageSuccessfullyDeadLettered()
        {
            await TestServiceBusQueueDeadLetteredMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential(), configureMessagePump: opt => opt.AutoComplete = false)
                       .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(opt => opt.AddMessageContextFilter(context => context.Properties["Topic"].ToString() == "Customers"))
                       .WithServiceBusMessageHandler<DeadLetterAzureServiceMessageHandler, Order>();
            });
        }

        private async Task TestServiceBusQueueDeadLetteredMessageAsync(Action<WorkerOptions> configureOptions)
        {
            // Arrange
            var options = new WorkerOptions();
            configureOptions(options);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                TestServiceMessageConsumer consumer = CreateQueueConsumer();
                await consumer.AssertDeadLetterMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusAbandon_PublishServiceBusMessage_MessageSuccessfullyAbandoned()
        {
            await TestServiceBusQueueAbandonMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential())
                       .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>(opt => opt.AddMessageContextFilter(_ => false))
                       .WithServiceBusMessageHandler<AbandonAzureServiceBusMessageHandler, Order>(opt => opt.AddMessageContextFilter(_ => true));
            });
        }

        private async Task TestServiceBusQueueAbandonMessageAsync(Action<WorkerOptions> configureOptions)
        {
            // Arrange
            var options = new WorkerOptions();
            configureOptions(options);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            // Act
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                // Assert
                TestServiceMessageConsumer consumer = CreateQueueConsumer();
                await consumer.AssertAbandonMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithCustomComplete_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential(), configureMessagePump: opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CompleteAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            // Act
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                // Assert
                TestServiceMessageConsumer consumer = CreateQueueConsumer();
                await consumer.AssertCompletedMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithAutoComplete_WhenNoMessageHandlerRegistered_ThenMessageShouldBeDeadLettered()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential(), configureMessagePump: opt => opt.AutoComplete = true);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            // Act
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                // Assert
                TestServiceMessageConsumer consumer = CreateQueueConsumer();
                await consumer.AssertDeadLetterMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithAutoComplete_WhenNoMessageHandlerAbleToHandle_ThenMessageShouldBeDeadLettered()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential(), configureMessagePump: opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<OrdersSabotageAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            // Act
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                // Assert
                TestServiceMessageConsumer consumer = CreateQueueConsumer();
                await consumer.AssertDeadLetterMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithAutoComplete_PublishServiceBusMessage_MessageSuccessfullyCompleted()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential(), configureMessagePump: opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            // Act
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                // Assert
                TestServiceMessageConsumer consumer = CreateQueueConsumer();
                await consumer.AssertCompletedMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithMultipleMessages_PublishesServiceBusMessages_AllMessagesSuccessfullyHandled()
        {
            // Arrange
            await using var topicSubscription = await TemporaryTopicSubscription.CreateIfNotExistsAsync(HostName, TopicName, Guid.NewGuid().ToString(), _logger);

            var options = new WorkerOptions();
            options.AddXunitTestLogging(_outputWriter);
            options.AddServiceBusTopicMessagePump(TopicName, topicSubscription.Name, HostName, new DefaultAzureCredential())
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage[] messages =
                Bogus.Make(50, () => CreateOrderServiceBusMessageForW3C()).ToArray();

            await using var worker = await Worker.StartNewAsync(options);
            var producer = TestServiceBusMessageProducer.CreateFor(TopicName, _config);

            // Act
            await producer.ProduceAsync(messages);

            // Assert
            foreach (ServiceBusMessage msg in messages)
            {
                OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(msg.MessageId);
                AssertReceivedOrderEventDataForW3C(msg, eventData);
            }
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithServiceBusDeadLetterDuringProcessing_ThenMessageShouldBeDeadLettered()
        {
            await TestServiceBusMessageHandlingAsync(pump =>
            {
                pump.WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(opt => opt.AddMessageContextFilter(context => context.Properties["Topic"].ToString() == "Customers"))
                    .WithServiceBusMessageHandler<DeadLetterAzureServiceMessageHandler, Order>();
            },
            async (message, consumer) =>
            {
                await consumer.AssertDeadLetterMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithServiceBusAbandonInProcessing_ThenMessageShouldBeAbandoned()
        {
            await TestServiceBusMessageHandlingAsync(
                pump =>
                {
                    pump.WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>(opt => opt.AddMessageContextFilter(_ => false))
                        .WithServiceBusMessageHandler<AbandonAzureServiceBusMessageHandler, Order>(opt => opt.AddMessageContextFilter(_ => true));
                },
                async (message, consumer) =>
                {
                    await consumer.AssertAbandonMessageAsync(message.MessageId);
                });
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithAutoComplete_WithMatchedMessageHandler_ThenMessageShouldBeCompleted()
        {
            await TestServiceBusMessageHandlingAsync(
                pump =>
                {
                    pump.WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
                },
                async (message, consumer) =>
                {
                    await consumer.AssertCompletedMessageAsync(message.MessageId);
                });
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithAutoComplete_WhenNoMessageHandlerRegistered_ThenMessageShouldBeDeadLettered()
        {
            await TestServiceBusMessageHandlingAsync(
                pump =>
                {
                },
                async (message, consumer) =>
                {
                    await consumer.AssertDeadLetterMessageAsync(message.MessageId);
                });
        }

        [Fact]
        public async Task ServiceBusMessagePump_WhenMessageHandlerIsSelectedButFailsToProcess_ThenMessageShouldBeAbandonedUntilDeadLettered()
        {
            await TestServiceBusMessageHandlingAsync(
                pump =>
                {
                    pump.WithServiceBusMessageHandler<OrdersSabotageAzureServiceBusMessageHandler, Order>();
                },
                async (message, consumer) =>
                {
                    await consumer.AssertAbandonMessageAsync(message.MessageId);
                    await consumer.AssertDeadLetterMessageAsync(message.MessageId);
                });
        }

        [Fact]
        public async Task ServiceBusMessagePump_WhenMessageHandlerIsNotSelectedWithoutFallback_ThenMessageShouldBeDeadLettered()
        {
            await TestServiceBusMessageHandlingAsync(
                pump =>
                {
                    pump.WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>();
                },
                async (message, consumer) =>
                {
                    await consumer.AssertDeadLetterMessageAsync(message.MessageId);
                });
        }

        private async Task TestServiceBusMessageHandlingAsync(
            Action<ServiceBusMessageHandlerCollection> configurePump,
            Func<ServiceBusMessage, TestServiceMessageConsumer, Task> assertion)
        {
            var options = new WorkerOptions();
            ServiceBusMessageHandlerCollection collection = options.AddServiceBusQueueMessagePump(QueueName, HostName, new DefaultAzureCredential());

            configurePump(collection);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                TestServiceMessageConsumer consumer = CreateQueueConsumer();
                await assertion(message, consumer);
            });
        }

        private TestServiceMessageConsumer CreateQueueConsumer()
        {
            var consumer = TestServiceMessageConsumer.CreateForQueue(QueueName, _serviceBusConfig, _logger);
            return consumer;
        }

        [Theory]
        [InlineData(TopicSubscription.None, false)]
        public async Task ServiceBusTopicMessagePump_WithNoneTopicSubscription_DoesNotCreateTopicSubscription(TopicSubscription topicSubscription, bool doesSubscriptionExists)
        {
            // Arrange
            var options = new WorkerOptions();
            var subscriptionName = $"Subscription-{Guid.NewGuid():N}";

            AddServiceBusTopicMessagePump(options, subscriptionName, topicSubscription)
                    .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            // Act
            await using var worker = await Worker.StartNewAsync(options);

            // Assert
            ServiceBusAdministrationClient adminClient = _serviceBusConfig.GetAdminClient();
            Response<bool> subscriptionExistsResponse = await adminClient.SubscriptionExistsAsync(TopicName, subscriptionName);
            Assert.Equal(doesSubscriptionExists, subscriptionExistsResponse.Value);
        }

        private ServiceBusMessageHandlerCollection AddServiceBusTopicMessagePump(
            WorkerOptions options,
            string subscriptionName,
            TopicSubscription topicSubscription)
        {
            var credential = new DefaultAzureCredential();

            options.AddXunitTestLogging(_outputWriter);
            return topicSubscription switch
            {
                TopicSubscription.None =>
                    options.AddServiceBusTopicMessagePump(
                        TopicName, subscriptionName, _ => new ServiceBusClient(HostName, credential), opt => opt.TopicSubscription = TopicSubscription.None),

                _ => throw new ArgumentOutOfRangeException(nameof(topicSubscription), topicSubscription, "Unknown topic subscription")
            };
        }
    }
}