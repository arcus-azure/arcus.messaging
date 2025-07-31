using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit
{
    public class MessageContextTests
    {
        [Fact]
        public void Constructor_NoPropertiesSpecified_ThrowsException()
        {
            // Arrange
            var messageId = Guid.NewGuid().ToString();
            var jobId = Guid.NewGuid().ToString();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MessageContext(messageId, jobId, properties: null));
        }

        [Fact]
        public void Constructor_NoMessageIdSpecified_ThrowsException()
        {
            // Arrange
            var properties = new Dictionary<string, object>();
            var jobId = Guid.NewGuid().ToString();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MessageContext(messageId: null, jobId, properties: properties));
        }

        [Fact]
        public void Constructor_Valid_Succeeds()
        {
            // Arrange
            var messageId = Guid.NewGuid().ToString();
            var jobId = Guid.NewGuid().ToString();
            var properties = new Dictionary<string, object>
            {
                {"CorrelationId", "ABC"}
            };

            // Act
            var messageContext = new MessageContext(messageId, jobId, properties);

            // Assert
            Assert.Equal(messageId, messageContext.MessageId);
            Assert.Equal(jobId, messageContext.JobId);
            Assert.Equal(properties.Count, messageContext.Properties.Count);
        }
    }
}