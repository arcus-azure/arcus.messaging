using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit
{
    [Trait("Category", "Unit")]
    public class MessageContextTests
    {
        [Fact]
        public void Constructor_NoPropertiesSpecified_ThrowsException()
        {
            // Arrange
            var messageId = Guid.NewGuid().ToString();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MessageContext(messageId, properties: null));
        }

        [Fact]
        public void Constructor_NoMessageIdSpecified_ThrowsException()
        {
            // Arrange
            var properties = new Dictionary<string, object>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MessageContext(messageId: null, properties: properties));
        }

        [Fact]
        public void Constructor_Valid_Succeeds()
        {
            // Arrange
            var messageId = Guid.NewGuid().ToString();
            var properties = new Dictionary<string, object>
            {
                {"CorrelationId", "ABC"}
            };

            // Act & Assert
            var messageContext = new MessageContext(messageId, properties);

            // Assert
            Assert.Equal(messageId, messageContext.MessageId);
            Assert.Equal(properties.Count, messageContext.Properties.Count);
        }
    }
}