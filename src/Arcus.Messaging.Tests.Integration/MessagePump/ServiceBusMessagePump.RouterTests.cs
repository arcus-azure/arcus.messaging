using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Core.Messages.v2;
using Arcus.Messaging.Tests.Workers.MessageBodyHandlers;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Arcus.Messaging.Tests.Integration.MessagePump.Fixture.ServiceBusTestContext;
using ServiceBusEntityType = Arcus.Messaging.Abstractions.ServiceBus.ServiceBusEntityType;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Fact]
        public async Task ServiceBusTopicMessagePump_WithContextFilter_SuccessfullyProcessesMessage()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            var contextProperty = new KeyValuePair<string, object>(Bogus.Lorem.Word(), Bogus.Lorem.Sentence());
            serviceBus.WhenServiceBusTopicMessagePump()
                      .WithMatchedServiceBusMessageHandler(handler =>
                      {
                          handler.AddMessageContextFilter(context => context.Properties.ContainsKey("NotExisting"));
                      })
                      .WithMatchedServiceBusMessageHandler(handler =>
                      {
                          handler.AddMessageContextFilter(context => context.Properties.Contains(contextProperty));
                      });

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync(message =>
            {
                message.WithApplicationProperty(contextProperty);
            });

            // Assert
            await serviceBus.ShouldConsumeViaMatchedHandlerAsync(messages);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePump_WithBodyFilter_SuccessfullyProcessesMessage()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string orderId = Bogus.Random.Guid().ToString();
            serviceBus.WhenServiceBusTopicMessagePump()
                      .WithMatchedServiceBusMessageHandler(handler => handler.AddMessageBodyFilter(order => order.Id == orderId));

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync(msg =>
            {
                msg.WithBody(order => order.Id = orderId);
            });

            // Assert
            await serviceBus.ShouldConsumeViaMatchedHandlerAsync(messages);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePump_WithBodySerializer_SuccessfullyProcessesMessage()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.Services.AddSingleton<OrderBatchMessageBodySerializer>();
            serviceBus.WhenServiceBusQueueMessagePump()
                      .WithMatchedServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(handler =>
                      {
                          handler.UseMessageBodyDeserializer<OrderBatchMessageBodySerializer>();
                      });

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync();

            // Assert
            await serviceBus.ShouldConsumeViaMatchedHandlerAsync(messages);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePump_WithContextAndBodyFilter_SuccessfullyProcessesMessage()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            string orderId = Bogus.Random.Guid().ToString();
            var contextProperty = new KeyValuePair<string, object>(Bogus.Lorem.Word(), Bogus.Lorem.Sentence());
            serviceBus.WhenServiceBusQueueMessagePump()
                      .WithMatchedServiceBusMessageHandler(handler =>
                      {
                          handler.AddMessageContextFilter(context => context.Properties.Contains(contextProperty))
                                 .AddMessageBodyFilter(order => order.Id == orderId);
                      });

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync(message =>
            {
                message.WithBody(order => order.Id = orderId)
                       .WithApplicationProperty(contextProperty);
            });

            // Assert
            await serviceBus.ShouldConsumeViaMatchedHandlerAsync(messages);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePump_WithContextAndBodyFilterAndBodySerializer_SuccessfullyProcessesMessage()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.Services.AddSingleton<OrderBatchMessageBodySerializer>();
            var contextProperty = new KeyValuePair<string, object>(Bogus.Lorem.Word(), Bogus.Lorem.Sentence());
            serviceBus.WhenServiceBusQueueMessagePump()
                      .WithMatchedServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(handler =>
                      {
                          handler.AddMessageContextFilter(context => context.Properties.Contains(contextProperty))
                                 .AddMessageBodyFilter(message => message.Orders.Length == 1)
                                 .UseMessageBodyDeserializer<OrderBatchMessageBodySerializer>();
                      });

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync(msg =>
            {
                msg.WithApplicationProperty(contextProperty);
            });

            // Assert
            await serviceBus.ShouldConsumeViaMatchedHandlerAsync(messages);
        }

        [Fact]
        public async Task ServiceBusQueueMessagePump_WithIgnoringMissingMembersDeserialization_SuccessfullyProcessesMessageByDifferentMessageTypeHandler()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenOnlyServiceBusQueueMessagePump(pump =>
            {
                pump.Routing.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore;

            }).WithMatchedServiceBusMessageHandler<WriteOrderV2ToDiskAzureServiceBusMessageHandler, OrderV2>();

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync();

            // Assert
            await serviceBus.ShouldConsumeViaMatchedHandlerAsync(messages);
        }

        [Theory]
        [InlineData(ServiceBusEntityType.Queue)]
        [InlineData(ServiceBusEntityType.Topic)]
        public async Task ServiceBusMessagePump_WithCustomMessageBodyEncoding_SuccessfullyProcessesMessage(ServiceBusEntityType entityType)
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusMessagePump(entityType)
                      .WithMatchedServiceBusMessageHandler();

            Encoding[] supportedEncodings = [Encoding.UTF8, Encoding.UTF32, Encoding.ASCII, Encoding.Unicode, Encoding.BigEndianUnicode];

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync(
                supportedEncodings.Select(MessageWithEncoding).ToArray());

            // Assert
            await serviceBus.ShouldConsumeViaMatchedHandlerAsync(messages);
        }

        private static Action<ServiceBusMessageBuilder> MessageWithEncoding(Encoding encoding)
        {
            return builder => builder.WithEncoding(encoding);
        }
    }
}
