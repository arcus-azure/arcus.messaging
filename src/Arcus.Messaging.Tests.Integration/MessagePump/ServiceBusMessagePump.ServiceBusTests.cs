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
using Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Fact]
        public async Task ServiceBusMessagePump_WithServiceBusDeadLetterDuringProcessing_ThenMessageShouldBeDeadLettered()
        {
            await TestServiceBusMessageHandlingAsync(pump =>
            {
                pump.WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties["Topic"].ToString() == "Customers")
                    .WithServiceBusMessageHandler<DeadLetterAzureServiceMessageHandler, Order>()
                    .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext _) => false);
            },
            async (message, consumer) =>
            {
                await consumer.AssertDeadLetterMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithServiceBusDeadLetterOnFallback_ThenMessageShouldBeDeadLettered()
        {
            await TestServiceBusMessageHandlingAsync(
                pump =>
                {
                    pump.WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>((AzureServiceBusMessageContext _) => true)
                        .WithServiceBusFallbackMessageHandler<DeadLetterAzureServiceBusFallbackMessageHandler>();
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
                    pump.WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                        .WithServiceBusMessageHandler<AbandonAzureServiceBusMessageHandler, Order>((AzureServiceBusMessageContext _) => true);
                },
                async (message, consumer) =>
                {
                    await consumer.AssertAbandonMessageAsync(message.MessageId);
                });
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithServiceBusAbandonOnFallback_ThenMessageSuccessShouldBeAbandoned()
        {
            await TestServiceBusMessageHandlingAsync(pump =>
                {
                    pump.WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                        .WithServiceBusFallbackMessageHandler<AbandonAzureServiceBusFallbackMessageHandler>();
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

        [Fact]
        public async Task ServiceBusMessagePump_WhenFallbackMessageHandlerSelectedSucceedsProcessing_ThenMessageShouldBeCompleted()
        {
            await TestServiceBusMessageHandlingAsync(
                pump =>
                {
                    pump.WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                        .WithServiceBusFallbackMessageHandler<PassThruAzureServiceBusFallbackMessageHandler>();
                },
                async (message, consumer) =>
                {
                    await consumer.AssertCompletedMessageAsync(message.MessageId);
                });
        }

        [Fact]
        public async Task ServiceBusMessagePump_WhenFallbackMessageHandlerSelectedButFailsToProcess_ThenMessageShouldBeAbandonedUntilDeadLettered()
        {
            await TestServiceBusMessageHandlingAsync(
                pump =>
                {
                    pump.WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                        .WithServiceBusFallbackMessageHandler<SabotageAzureServiceBusFallbackMessageHandler>();
                },
                async (message, consumer) =>
                {
                    await consumer.AssertAbandonMessageAsync(message.MessageId);
                    await consumer.AssertDeadLetterMessageAsync(message.MessageId);
                });
        }

        private async Task TestServiceBusMessageHandlingAsync(
            Action<ServiceBusMessageHandlerCollection> configurePump,
            Func<ServiceBusMessage, TestServiceMessageConsumer, Task> assertion)
        {
            var options = new WorkerOptions();
            ServiceBusMessageHandlerCollection collection = options.AddServiceBusQueueMessagePump(_ => QueueConnectionString);

            configurePump(collection);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue, message, async () =>
            {
                var consumer = TestServiceMessageConsumer.CreateForQueue(_config, _logger);
                await assertion(message, consumer);
            });
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithMultipleMessages_PublishesServiceBusMessages_AllMessagesSuccessfullyHandled()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePump(TopicConnectionString)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            options.AddXunitTestLogging(_outputWriter);

            ServiceBusMessage[] messages = 
                Bogus.Make(50, () => CreateOrderServiceBusMessageForW3C()).ToArray();

            await using var worker = await Worker.StartNewAsync(options);
            var producer = TestServiceBusMessageProducer.CreateFor(_config, ServiceBusEntityType.Topic);

            // Act
            await producer.ProduceAsync(messages);

            // Assert
            foreach (ServiceBusMessage msg in messages)
            {
                OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(msg.MessageId);
                AssertReceivedOrderEventDataForW3C(msg, eventData);
            }
        }

        [Theory]
        [InlineData(TopicSubscription.None, false)]
        [InlineData(TopicSubscription.Automatic, true)]
        public async Task ServiceBusTopicMessagePump_WithNoneTopicSubscription_DoesNotCreateTopicSubscription(TopicSubscription topicSubscription, bool doesSubscriptionExists)
        {
            // Arrange
            var options = new WorkerOptions();
            var subscriptionName = $"Subscription-{Guid.NewGuid():N}";
            options.AddServiceBusTopicMessagePump(
                       subscriptionName, 
                       _ => TopicConnectionString, 
                       opt => opt.TopicSubscription = topicSubscription)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            
            // Act
            await using var worker = await Worker.StartNewAsync(options);
            
            // Assert
            var client = new ServiceBusAdministrationClient(TopicConnectionString);
            var properties = ServiceBusConnectionStringProperties.Parse(TopicConnectionString);
                
            Response<bool> subscriptionExistsResponse = await client.SubscriptionExistsAsync(properties.EntityPath, subscriptionName);
            Assert.Equal(doesSubscriptionExists, subscriptionExistsResponse.Value);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithSubscriptionNameOver50_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(ServiceBusEntityType.Topic, options =>
            {
                options.AddServiceBusTopicMessagePump(
                           subscriptionName: "Test-Receive-All-Topic-Only-with-an-azure-servicebus-topic-subscription-name-over-50-characters", 
                           _ => TopicConnectionString,
                           opt =>
                           {
                               opt.AutoComplete = true;
                               opt.TopicSubscription = TopicSubscription.Automatic;
                           })
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }
    }
}
