using System;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageBodySerializers;
using Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageHandlers;
using Arcus.Messaging.Tests.Workers.MessageBodyHandlers;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class EventHubsMessagePumpTests
    {
        [Fact]
        public async Task EventHubsMessagePumpWithMessageContextFilter_PublishesMessage_MessageSuccessfullyProcessed()
        {
            await TestEventHubsMessageHandlingAsync(options =>
            {
                AddEventHubsMessagePump(options)
                    .WithEventHubsMessageHandler<TestEventHubsMessageHandler<SensorReading>, SensorReading>(messageContextFilter: _ => false)
                    .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();
            });
        }

        [Fact]
        public async Task EventHubsMessagePumpWithMessageFilter_PublishesMessage_MessageSuccessfullyProcessed()
        {
            await TestEventHubsMessageHandlingAsync(options =>
            {
                AddEventHubsMessagePump(options)
                    .WithEventHubsMessageHandler<TestEventHubsMessageHandler<SensorReading>, SensorReading>(messageBodyFilter: _ => false)
                    .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();
            });
        }

        [Fact]
        public async Task EventHubsMessagePumpWithDifferentMessageType_PublishesMessage_MessageSuccessfullyProcessed()
        {
            await TestEventHubsMessageHandlingAsync(options =>
            {
                AddEventHubsMessagePump(options)
                    .WithEventHubsMessageHandler<TestEventHubsMessageHandler<Shipment>, Shipment>()
                    .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();
            });
        }

        [Fact]
        public async Task EventHubsMessagePumpWithMessageBodySerializer_PublishesMessage_MessageSuccessfullyProcessed()
        {
            await TestEventHubsMessageHandlingAsync(options =>
            {
                AddEventHubsMessagePump(options)
                    .WithEventHubsMessageHandler<SensorReadingBatchEventHubsMessageHandler, SensorReadingBatch>(
                        messageBodySerializerImplementationFactory: provider =>
                        {
                            var logger = provider.GetRequiredService<ILogger<SensorReadingBatchBodySerializer>>();
                            return new SensorReadingBatchBodySerializer(logger);
                        });
            });
        }

        [Fact]
        public async Task EventHubsMessagePumpWithFallback_PublishesMessage_MessageSuccessfullyProcessed()
        {
            await TestEventHubsMessageHandlingAsync(options =>
            {
                AddEventHubsMessagePump(options)
                    .WithEventHubsMessageHandler<TestEventHubsMessageHandler<Shipment>, Shipment>()
                    .WithFallbackMessageHandler<SabotageEventHubsFallbackMessageHandler, AzureEventHubsMessageContext>()
                    .WithFallbackMessageHandler<WriteSensorToDiskEventHubsMessageHandler>();
            });
        }

        [Fact]
        public async Task EventHubsMessagePumpWithAllFiltersAndOptions_PublishesMessage_MessageSuccessfullyProcessed()
        {
            await TestEventHubsMessageHandlingAsync(options =>
            {
                AddEventHubsMessagePump(options)
                    .WithEventHubsMessageHandler<TestEventHubsMessageHandler<Shipment>, Shipment>()
                    .WithEventHubsMessageHandler<TestEventHubsMessageHandler<SensorReading>, SensorReading>(messageBodyFilter: _ => false)
                    .WithEventHubsMessageHandler<TestEventHubsMessageHandler<SensorReading>, SensorReading>(messageContextFilter: _ => false)
                    .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>(
                        messageContextFilter: context => context.ConsumerGroup == "$Default" 
                                                         && context.EventHubsName == EventHubsName
                                                         && context.EventHubsNamespace == FullyQualifiedEventHubsNamespace,
                        messageBodySerializerImplementationFactory: provider =>
                        {
                            var logger = provider.GetService<ILogger<OrderBatchMessageBodySerializer>>();
                            return new OrderBatchMessageBodySerializer(logger);
                        },
                        messageBodyFilter: order => Guid.TryParse(order.SensorId, out Guid _),
                        messageHandlerImplementationFactory: provider =>
                        {
                            return new WriteSensorToDiskEventHubsMessageHandler(
                                provider.GetRequiredService<IMessageCorrelationInfoAccessor>(),
                                provider.GetRequiredService<ILogger<WriteSensorToDiskEventHubsMessageHandler>>());
                        });
            });
        }

        [Fact]
        public async Task EventHubsMessagePumpWithoutSameJobId_PublishesMessage_MessageFailsToBeProcessed()
        {
            await Assert.ThrowsAsync<TimeoutException>(() =>
            {
                return TestEventHubsMessageHandlingAsync(options =>
                {
                    EventHubsMessageHandlerCollection collection = AddEventHubsMessagePump(options);
                    Assert.False(string.IsNullOrWhiteSpace(collection.JobId));

                    var otherCollection = new EventHubsMessageHandlerCollection(new ServiceCollection())
                    {
                        JobId = Guid.NewGuid().ToString()
                    };

                    otherCollection.WithEventHubsMessageHandler<TestEventHubsMessageHandler<SensorReading>, SensorReading>(messageContextFilter: _ => false)
                                   .WithEventHubsMessageHandler<WriteSensorToDiskEventHubsMessageHandler, SensorReading>();
                });
            });
        }
    }
}
