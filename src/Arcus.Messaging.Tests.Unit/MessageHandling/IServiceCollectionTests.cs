using System;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    public class IServiceCollectionTests
    {
        [Fact]
        public void WithFallbackMessageHandler_WithValidType_RegistersInterface()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.WithFallbackMessageHandler<PassThruFallbackMessageHandler>();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var messageHandler = provider.GetRequiredService<IFallbackMessageHandler>();

            Assert.IsType<PassThruFallbackMessageHandler>(messageHandler);
        }

        [Fact]
        public void WithFallbackMessageHandler_WithValidImplementationFunction_RegistersInterface()
        {
            // Arrange
            var services = new ServiceCollection();
            var expected = new PassThruFallbackMessageHandler();

            // Act
            services.WithFallbackMessageHandler(serviceProvider => expected);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var actual = provider.GetRequiredService<IFallbackMessageHandler>();

            Assert.Same(expected, actual);
        }

        [Fact]
        public void WithFallbackMessageHandlerType_WithoutServices_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((IServiceCollection) null).WithFallbackMessageHandler<PassThruFallbackMessageHandler>());
        }

        [Fact]
        public void WithFallbackMessageHandlerImplementationFunction_WithoutServices_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => ((IServiceCollection) null).WithFallbackMessageHandler(serviceProvider => new PassThruFallbackMessageHandler()));
        }

        [Fact]
        public void WithFallbackMessageHandlerImplementationFunction_WithoutImplementationFunction_Throws()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithFallbackMessageHandler(createImplementation: (Func<IServiceProvider, PassThruFallbackMessageHandler>) null));
        }
    }
}
