﻿using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Messaging.Tests.Unit.Pumps.ServiceBus.Stubs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.Pumps.ServiceBus.Extensions
{
    public class MessageContextIServiceCollectionExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithServiceBusMessageHandler_WithMessageContextFilter_UsesFilter(bool matchesContext)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedContext = AzureServiceBusMessageContextFactory.Generate();
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.NotSame(expectedHandler, handler);
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithMessageContextFilter_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageContextFilter: null));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WithServiceBusMessageHandler_WithMessageContextFilterWithImplementationFactory_UsesFilter(bool matchesContext)
        {
            // Arrange
            var services = new ServiceCollection();
            var expectedContext = AzureServiceBusMessageContextFactory.Generate();
            var expectedHandler = new TestServiceBusMessageHandler();

            // Act
            services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                messageContextFilter: context =>
                {
                    Assert.Same(expectedContext, context);
                    return matchesContext;
                },
                implementationFactory: serviceProvider => expectedHandler);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<MessageHandler> handlers = MessageHandler.SubtractFrom(provider, NullLogger.Instance);
            Assert.NotNull(handlers);
            MessageHandler handler = Assert.Single(handlers);
            Assert.NotNull(handler);
            Assert.Same(expectedHandler, handler.GetMessageHandlerInstance());
            Assert.Equal(matchesContext, handler.CanProcessMessageBasedOnContext(expectedContext));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithoutMessageContextFilterWithImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageContextFilter: null,
                    implementationFactory: serviceProvider => new TestServiceBusMessageHandler()));
        }

        [Fact]
        public void WithServiceBusMessageHandler_WithMessageContextFilterWithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.WithServiceBusMessageHandler<TestServiceBusMessageHandler, TestMessage>(
                    messageContextFilter: context => true,
                    implementationFactory: null));
        }
    }
}
