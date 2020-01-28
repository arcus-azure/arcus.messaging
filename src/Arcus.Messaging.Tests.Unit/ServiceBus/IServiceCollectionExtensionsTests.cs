using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddServiceBusTopicMessagePump_WithSubscriptionNameIndirectSecretProvider_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            IServiceCollection result = 
                services.AddServiceBusTopicMessagePump<EmptyMessagePump>(
                    "subscription name", "secret name", configureMessagePump: options => options.AutoComplete = true);
            
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AddServiceBusTopicMessagePump_WithTopicNameAndSubscriptionNameIndirectSecretProvider_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            IServiceCollection result = 
                services.AddServiceBusTopicMessagePump<EmptyMessagePump>(
                    "topic name", "subscription name", "secret name", configureMessagePump: options => options.AutoComplete = true);
            
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void AddServiceBusQueueMessagePump_IndirectSecretProvider_WiresUpCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            IServiceCollection result = 
                services.AddServiceBusQueueMessagePump<EmptyMessagePump>(
                    "queue name", "secret name", configureMessagePump: options => options.AutoComplete = true);
            
            // Assert
            Assert.NotNull(result);
        }
    }
}
