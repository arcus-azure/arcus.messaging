using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions;
using Microsoft.Azure.ServiceBus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    [Trait("Category", "Unit")]
    public class MessageExtensionsTests
    {
        [Fact]
        public void GetUserProperty_WithExistingUserProperty_ReturnsCastType()
        {
            // Arrange
            const string key = "uri-key";
            var serviceBusMessage = new Message
            {
                UserProperties = { [key] = new Uri("http://localhost") }
            };

            // Act
            var uri = serviceBusMessage.GetUserProperty<Uri>(key);

            // Assert
            Assert.NotNull(uri);
        }

        [Fact]
        public void GetUserProperty_WithNonExistingKey_ThrowsKeyNotFound()
        {
            // Arrange
            var serviceBusMessage = new Message();

            // Act / Assert
            Assert.Throws<KeyNotFoundException>(
                () => serviceBusMessage.GetUserProperty<Uri>("non-existing-key"));
        }

        [Fact]
        public void GetUserProperty_WithWrongUserPropertyValue_ThrowsInvalidCast()
        {
            // Arrange
            const string key = "uri-key";
            var serviceBusMessage = new Message
            {
                UserProperties = { [key] = TimeSpan.Zero }
            };

            // Act / Assert
            Assert.Throws<InvalidCastException>(
                () => serviceBusMessage.GetUserProperty<Uri>(key));
        }
    }
}
