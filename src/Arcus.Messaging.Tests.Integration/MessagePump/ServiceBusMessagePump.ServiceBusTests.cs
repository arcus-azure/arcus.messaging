using System.Threading.Tasks;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.MessagePump
{
    public partial class ServiceBusMessagePumpTests
    {
        [Fact]
        public async Task ServiceBusTopicMessagePump_WithCustomComplete_SuccessfullyProcessesMessage()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusTopicMessagePump(pump => pump.Routing.AutoComplete = false)
                      .WithMatchedServiceBusMessageHandler<CompleteAzureServiceBusMessageHandler>();

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync();

            // Assert
            await serviceBus.ShouldCompleteConsumedAsync(messages);
        }

        [Fact]
        public async Task ServiceBusTopicMessagePump_WithMultipleMessages_SuccessfullyProcessesAllMessages()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusTopicMessagePump()
                      .WithMatchedServiceBusMessageHandler();

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync(50);

            // Assert
            await serviceBus.ShouldConsumeViaMatchedHandlerAsync(messages);
            await serviceBus.ShouldCompleteConsumedAsync(messages);
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithServiceBusDeadLetterDuringProcessing_ThenMessageShouldBeDeadLettered()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump()
                      .WithMatchedServiceBusMessageHandler<DeadLetterAzureServiceMessageHandler>();

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync();

            // Assert
            await serviceBus.ShouldNotConsumeButDeadLetterAsync(messages);
        }

        [Fact]
        public async Task ServiceBusMessagePump_WithServiceBusAbandonInProcessing_ThenMessageShouldBeAbandoned()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump()
                      .WithMatchedServiceBusMessageHandler<AbandonAzureServiceBusMessageHandler>();

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync();

            // Assert
            await serviceBus.ShouldNotConsumeButAbandonAsync(messages);
        }

        [Fact]
        public async Task ServiceBusMessagePumpWithAutoComplete_WithMatchedMessageHandler_ThenMessageShouldBeCompleted()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump(pump => pump.Routing.AutoComplete = true)
                      .WithMatchedServiceBusMessageHandler();

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync();

            // Assert
            await serviceBus.ShouldConsumeViaMatchedHandlerAsync(messages);
            await serviceBus.ShouldCompleteConsumedAsync(messages);
        }

        [Fact]
        public async Task ServiceBusMessagePump_WhenNoMessageHandlerRegistered_ThenMessageShouldBeDeadLettered()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump();

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync();

            // Assert
            await serviceBus.ShouldNotConsumeButDeadLetterAsync(messages);
        }

        [Fact]
        public async Task ServiceBusMessagePump_WhenMessageHandlerIsSelectedButFailsToProcess_ThenMessageShouldBeAbandonedUntilDeadLettered()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump()
                      .WithMatchedServiceBusMessageHandler<OrdersSabotageAzureServiceBusMessageHandler>();

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync();

            // Assert
            await serviceBus.ShouldNotConsumeButAbandonAsync(messages);
            await serviceBus.ShouldNotConsumeButDeadLetterAsync(messages);
        }

        [Fact]
        public async Task ServiceBusMessagePump_WhenMessageHandlerIsNotSelected_ThenMessageShouldBeDeadLettered()
        {
            // Arrange
            await using var serviceBus = GivenServiceBus();

            serviceBus.WhenServiceBusQueueMessagePump()
                      .WithUnrelatedServiceBusMessageHandler();

            // Act
            var messages = await serviceBus.WhenProducingMessagesAsync();

            // Assert
            await serviceBus.ShouldNotConsumeButDeadLetterAsync(messages);
        }
    }
}