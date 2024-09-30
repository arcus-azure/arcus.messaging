using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
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
                options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = false)
                       .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties["Topic"].ToString() == "Customers")
                       .WithServiceBusMessageHandler<DeadLetterAzureServiceMessageHandler, Order>()
                       .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext _) => false);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetterOnFallback_PublishServiceBusMessage_MessageSuccessfullyDeadLettered()
        {
            await TestServiceBusQueueDeadLetteredMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = false)
                       .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>((AzureServiceBusMessageContext _) => true)
                       .WithServiceBusFallbackMessageHandler<DeadLetterAzureServiceBusFallbackMessageHandler>();
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
                options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName)
                       .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                       .WithServiceBusMessageHandler<AbandonAzureServiceBusMessageHandler, Order>((AzureServiceBusMessageContext _) => true);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusAbandonOnFallback_PublishServiceBusMessage_MessageSuccessfullyAbandoned()
        {
            await TestServiceBusQueueAbandonMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePump(QueueName, HostName)
                       .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                       .WithServiceBusFallbackMessageHandler<AbandonAzureServiceBusFallbackMessageHandler>();
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
        public async Task ServiceBusQueueMessagePumpWithCustomCompleteOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
                   .WithServiceBusFallbackMessageHandler<CompleteAzureServiceBusFallbackMessageHandler>();
            
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
        public async Task ServiceBusTopicMessagePumpWithCustomComplete_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = false)
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

        private TestServiceMessageConsumer CreateQueueConsumer()
        {
            var consumer = TestServiceMessageConsumer.CreateForQueue(QueueName, _serviceBusConfig, _logger);
            return consumer;
        }

        [Theory]
        [InlineData(TopicSubscription.None, false)]
        [InlineData(TopicSubscription.Automatic, true)]
        public async Task ServiceBusTopicMessagePump_WithNoneTopicSubscription_DoesNotCreateTopicSubscription(TopicSubscription topicSubscription, bool doesSubscriptionExists)
        {
            // Arrange
            var options = new WorkerOptions();
            var subscriptionName = $"Subscription-{Guid.NewGuid():N}";
            options.AddServiceBusTopicMessagePumpUsingManagedIdentity(
                       TopicName,
                       subscriptionName, 
                       HostName, 
                       configureMessagePump: opt => opt.TopicSubscription = topicSubscription)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            
            // Act
            await using var worker = await Worker.StartNewAsync(options);
            
            // Assert
            ServiceBusAdministrationClient client = _serviceBusConfig.GetAdminClient();
            Response<bool> subscriptionExistsResponse = await client.SubscriptionExistsAsync(TopicName, subscriptionName);
            Assert.Equal(doesSubscriptionExists, subscriptionExistsResponse.Value);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithSubscriptionNameOver50_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Topic, options =>
            {
                options.AddServiceBusTopicMessagePumpUsingManagedIdentity(
                           TopicName,
                           subscriptionName: "Test-Receive-All-Topic-Only-with-an-azure-servicebus-topic-subscription-name-over-50-characters", 
                           HostName,
                           configureMessagePump: opt =>
                           {
                               opt.AutoComplete = true;
                               opt.TopicSubscription = TopicSubscription.Automatic;
                           })
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }
    }
}
