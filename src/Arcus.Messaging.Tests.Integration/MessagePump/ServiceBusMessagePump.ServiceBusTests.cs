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
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
         [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetter_PublishServiceBusMessage_MessageSuccessfullyDeadLettered()
        {
            await TestServiceBusQueueDeadLetteredMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = false)
                       .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties["Topic"].ToString() == "Customers")
                       .WithServiceBusMessageHandler<DeadLetterAzureServiceMessageHandler, Order>(context => context.Properties["Topic"].ToString() == "Orders")
                       .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext _) => false);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusDeadLetterOnFallback_PublishServiceBusMessage_MessageSuccessfullyDeadLettered()
        {
            await TestServiceBusQueueDeadLetteredMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = false)
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

            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue, message, async () =>
            {
                var consumer = TestServiceMessageConsumer.CreateForQueue(_config, _logger);
                await consumer.AssertDeadLetterMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusAbandon_PublishServiceBusMessage_MessageSuccessfullyAbandoned()
        {
            await TestServiceBusQueueAbandonMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString)
                       .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                       .WithServiceBusMessageHandler<AbandonAzureServiceBusMessageHandler, Order>((AzureServiceBusMessageContext _) => true);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithServiceBusAbandonOnFallback_PublishServiceBusMessage_MessageSuccessfullyAbandoned()
        {
            await TestServiceBusQueueAbandonMessageAsync(options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString)
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
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue, message, async () =>
            {
                // Assert
                var consumer = TestServiceMessageConsumer.CreateForQueue(_config, _logger);
                await consumer.AssertAbandonMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithCustomCompleteOnFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>()
                   .WithServiceBusFallbackMessageHandler<CompleteAzureServiceBusFallbackMessageHandler>();
            
            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();
            
            // Act
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue, message, async () =>
            {
                // Assert
                var consumer = TestServiceMessageConsumer.CreateForQueue(_config, _logger);
                await consumer.AssertCompletedMessageAsync(message.MessageId);
            });
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithCustomComplete_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = false)
                   .WithServiceBusMessageHandler<CompleteAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();

            // Act
            await TestServiceBusMessageHandlingAsync(options, ServiceBusEntityType.Queue, message, async () =>
            {
                // Assert
                var consumer = TestServiceMessageConsumer.CreateForQueue(_config, _logger);
                await consumer.AssertCompletedMessageAsync(message.MessageId);
            });
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
