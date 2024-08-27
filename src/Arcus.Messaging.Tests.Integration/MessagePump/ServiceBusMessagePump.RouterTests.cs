using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Tests.Core.Events.v1;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Workers.MessageBodyHandlers;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.ServiceBus.DiskMessageEventConsumer;
using static Microsoft.Extensions.Logging.ServiceBusEntityType;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Fact]
        public async Task ServiceBusTopicMessagePumpWithBodyFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Topic, options =>
            {
                options.AddServiceBusTopicMessagePump(TopicConnectionString)
                       .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>((Customer body) => body is null)
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>((Order body) => body.Id != null)
                       .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((Order _) => false);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextAndBodyFilteringWithSerializer_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>(messageContextFilter: _ => false)
                       .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(messageBodyFilter: _ => true)
                       .WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(
                           messageContextFilter: context => context != null,
                           messageBodySerializerImplementationFactory: serviceProvider =>
                           {
                               var logger = serviceProvider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                               return new OrderBatchMessageBodySerializer(logger);
                           },
                           messageBodyFilter: message => message.Orders.Length == 1);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextTypeFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextAndBodyFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.ContainsKey("NotExisting"), _ => false)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>(
                       context => context.Properties["Topic"].ToString() == "Orders", 
                       body => body.Id != null);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();
            message.ApplicationProperties["Topic"] = "Orders";

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData);
            });
        }

        [Fact]
        public async Task ServiceBusTopicMessagePumpWithContextFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePump(TopicConnectionString)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Customers")
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Orders")
                   .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((AzureServiceBusMessageContext _) => false);

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C();
            message.ApplicationProperties["Topic"] = "Orders";

            // Act / Assert
            await TestServiceBusMessageHandlingAsync(options, Topic, message, async () =>
            {
                OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithBatchedMessages_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(
                           messageBodySerializerImplementationFactory: serviceProvider =>
                           {
                               var logger = serviceProvider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                               return new OrderBatchMessageBodySerializer(logger);
                           });
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithIgnoringMissingMembersDeserialization_PublishesServiceBusMessage_MessageGetsProcessedByDifferentMessageHandler()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePump(
                           _ => QueueConnectionString, 
                           opt => opt.Routing.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore)
                       .WithServiceBusMessageHandler<WriteOrderV2ToDiskAzureServiceBusMessageHandler, OrderV2>();
            });
        }

        public static IEnumerable<object[]> Encodings
        {
            get
            {
                yield return new object[] { Encoding.UTF8 };
                yield return new object[] { Encoding.UTF32 };
                yield return new object[] { Encoding.ASCII };
                yield return new object[] { Encoding.Unicode };
                yield return new object[] { Encoding.BigEndianUnicode };
            }
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task ServiceBusQueueMessagePump_PublishesEncodedServiceBusMessage_MessageSuccessfullyProcessed(Encoding encoding)
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(encoding: encoding);

            // Act
            await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
            {
                // Assert
                OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData);
            });
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task ServiceBusTopicMessagePump_PublishesEncodedServiceBusMessage_MessageSuccessfullyProcessed(Encoding encoding)
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePump(TopicConnectionString)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(encoding: encoding);

            // Act
            await TestServiceBusMessageHandlingAsync(options, Topic, message, async () =>
            {
                // Assert
                OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
                AssertReceivedOrderEventDataForW3C(message, eventData);
            });
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                       .WithFallbackMessageHandler<WriteOrderToDiskFallbackMessageHandler>();
            });
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithServiceBusFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePump(_ => QueueConnectionString, opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>((AzureServiceBusMessageContext _) => false)
                       .WithServiceBusFallbackMessageHandler<WriteOrderToDiskFallbackMessageHandler>();
            });
        }
    }
}
