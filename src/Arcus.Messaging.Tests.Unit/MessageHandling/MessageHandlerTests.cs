using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    [Trait("Category", "Unit")]
    public class MessageHandlerTests
    {
        [Fact]
        public void SubtractsMessageHandlers_SelectsAllRegistrations()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IMessageHandler<string, MessageContext>>());
            services.AddSingleton(Mock.Of<IMessageHandler<int, MessageContext>>());
            services.AddSingleton(Mock.Of<IMessageHandler<TimeSpan, MessageContext>>());
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Act
            IEnumerable<MessageHandler> messageHandlers = MessageHandler.SubtractFrom(serviceProvider);

            // Assert
            Assert.Equal(3, messageHandlers.Count());
        }
    }
}
