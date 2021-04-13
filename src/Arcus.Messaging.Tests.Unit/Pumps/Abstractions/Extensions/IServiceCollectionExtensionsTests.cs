using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.Pumps.Abstractions.Extensions
{
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void WithMessageRouting_WithAutoCreation_RegistersMessageRouter()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMessageRouting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageRouter>());
        }

        [Fact]
        public void WithMessageRoutingCustomRouter_WithImplementationFactory_RegistersRouter()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMessageRouting(serviceProvider => new TestMessageRouter(serviceProvider, NullLogger.Instance));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IMessageRouter>());
        }

        [Fact]
        public void WithMessageRouting_WithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddMessageRouting<TestMessageRouter>(implementationFactory: null));
        }
    }
}
