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
                options.AddServiceBusTopicMessagePumpUsingManagedIdentity(TopicName, HostName)
                       .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(opt => opt.AddMessageBodyFilter(body => body is null))
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>(opt => opt.AddMessageBodyFilter(body => body.Id != null))
                       .WithMessageHandler<PassThruOrderMessageHandler, Order, AzureServiceBusMessageContext>((Order _) => false);
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextAndBodyFilteringWithSerializer_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>(opt => opt.AddMessageContextFilter(_ => false))
                       .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(opt => opt.AddMessageBodyFilter(_ => true))
                       .WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(
                           opt => opt.AddMessageContextFilter(context => context != null)
                                     .AddMessageBodyFilter(message => message.Orders.Length == 1)
                                     .AddMessageBodySerializer(serviceProvider =>
                                     {
                                         var logger = serviceProvider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                                         return new OrderBatchMessageBodySerializer(logger);
                                     }));
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextTypeFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>()
                       .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithContextAndBodyFiltering_RoutesServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(opt =>
                   {
                       opt.AddMessageContextFilter(context => context.Properties.ContainsKey("NotExisting"))
                          .AddMessageBodyFilter(_ => false);
                   })
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>(opt =>
                   {
                       opt.AddMessageContextFilter(context => context.Properties["Topic"].ToString() == "Orders")
                          .AddMessageBodyFilter(body => body.Id != null);
                   });

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
            options.AddServiceBusTopicMessagePumpUsingManagedIdentity(TopicName, HostName)
                   .WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(opt =>
                   {
                       opt.AddMessageContextFilter(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Customers");
                   })
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>(opt =>
                   {
                       opt.AddMessageContextFilter(context => context.Properties.TryGetValue("Topic", out object value) && value.ToString() == "Orders");
                   })
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
                options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(
                           opt => opt.AddMessageBodySerializer(serviceProvider =>
                           {
                               var logger = serviceProvider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                               return new OrderBatchMessageBodySerializer(logger);
                           }));
            });
        }

        [Fact]
        public async Task ServiceBusQueueMessagePumpWithIgnoringMissingMembersDeserialization_PublishesServiceBusMessage_MessageGetsProcessedByDifferentMessageHandler()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt =>
                {
                    opt.Routing.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore;

                }).WithServiceBusMessageHandler<WriteOrderV2ToDiskAzureServiceBusMessageHandler, OrderV2>();
            });
        }

        private static Encoding[] SupportedEncodings =>
        [
            Encoding.UTF8,
            Encoding.UTF32,
            Encoding.ASCII,
            Encoding.Unicode,
            Encoding.BigEndianUnicode
        ];

        [Fact]
        public async Task ServiceBusQueueMessagePump_PublishesEncodedServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = true)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            foreach (Encoding encoding in SupportedEncodings)
            {
                _logger.LogTrace("Use encoding '{Encoding}'", encoding.EncodingName);
                ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(encoding: encoding);

                // Act
                await TestServiceBusMessageHandlingAsync(options, Queue, message, async () =>
                {
                    // Assert
                    OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
                    AssertReceivedOrderEventDataForW3C(message, eventData);
                });
            }
        }

        [Fact]
        public async Task ServiceBusTopicMessagePump_PublishesEncodedServiceBusMessage_MessageSuccessfullyProcessed()
        {
            // Arrange
            var options = new WorkerOptions();
            options.AddServiceBusTopicMessagePumpUsingManagedIdentity(TopicName, HostName)
                   .WithServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler, Order>();

            foreach (Encoding encoding in SupportedEncodings)
            {
                _logger.LogTrace("Use encoding '{Encoding}'", encoding.EncodingName);
                ServiceBusMessage message = CreateOrderServiceBusMessageForW3C(encoding: encoding);

                // Act
                await TestServiceBusMessageHandlingAsync(options, Topic, message, async () =>
                {
                    // Assert
                    OrderCreatedEventData eventData = await ConsumeOrderCreatedAsync(message.MessageId);
                    AssertReceivedOrderEventDataForW3C(message, eventData);
                }); 
            }
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>(opt => opt.AddMessageContextFilter(_ => false))
                       .WithFallbackMessageHandler<WriteOrderToDiskFallbackMessageHandler>();
            });
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithServiceBusFallback_PublishServiceBusMessage_MessageSuccessfullyProcessed()
        {
            await TestServiceBusMessageHandlingAsync(Queue, options =>
            {
                options.AddServiceBusQueueMessagePumpUsingManagedIdentity(QueueName, HostName, configureMessagePump: opt => opt.AutoComplete = true)
                       .WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>(opt => opt.AddMessageContextFilter(_ => false))
                       .WithServiceBusFallbackMessageHandler<WriteOrderToDiskFallbackMessageHandler>();
            });
        }
    }
}
