#if NET6_0
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
#endif
namespace Arcus.Messaging.Tests.Unit.MessageHandling.EventHubs.Extensions
{
#if NET6_0
    public class AzureEventHubsServiceCollectionExtensionsTests
    {
        [Fact]
        public void Add_WithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            var collection = new EventHubsMessageHandlerCollection(services);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                collection.WithEventHubsMessageHandler<OrderEventHubsMessageHandler, Order>(implementationFactory: null));
        }
    }
#endif
}
