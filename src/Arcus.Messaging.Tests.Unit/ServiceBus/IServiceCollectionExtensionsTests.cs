using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Unit.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void WithServiceBusMessageHandler_WithBodyFilterWithoutContextFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutBodyFilterWithContextFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageBodyFilter: null,
                    messageContextFilter: context => true));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutImplementationFactory_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: null));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutImplementationFactoryWithContextFilterWithBodyFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: null,
                    messageContextFilter: context => true,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithImplementationFactoryWithoutContextFilterWithBodyFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: servivceProvider => new TestServiceBusMessageHandler(),
                    messageContextFilter: null,
                    messageBodyFilter: body => true));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithImplementationFactoryWithContextFilterWithoutBodyFilter_Throws()
        {
            // Arrange
            var collection = new ServiceBusMessageHandlerCollection(new ServiceCollection());

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    implementationFactory: serviceProvider => new TestServiceBusMessageHandler(),
                    messageContextFilter: context => true,
                    messageBodyFilter: null));
        }
    }
}
