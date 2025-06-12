using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    public class MessageRouterTests
    {
        [Fact]
        public void CreateWithoutOptionsAndLogger_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new MessageRouter(serviceProvider: null));
        }

        [Fact]
        public void CreateWithoutOptions_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                new MessageRouter(serviceProvider: null, logger: NullLogger<MessageRouter>.Instance));
        }

        [Fact]
        public void CreateWithoutLogger_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new MessageRouter(serviceProvider: null, options: new MessageRouterOptions()));
        }

        [Fact]
        public void Create_WithoutServiceProvider_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new MessageRouter(serviceProvider: null, options: new MessageRouterOptions(), logger: NullLogger<MessageRouter>.Instance));
        }
    }
}
