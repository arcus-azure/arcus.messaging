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

        [Fact]
        public void GetMessageCorrelationInfo_WithCorrelationIdAndTransactionIdAsUserProperties_ReturnsCorrelationInfo()
        {
            // Arrange
            string expectedOperationId = $"operation-{Guid.NewGuid()}";
            string expectedTransactionId = $"transaction-{Guid.NewGuid()}";
            var message = new Message
            {
                CorrelationId = expectedOperationId,
                UserProperties = { [PropertyNames.TransactionId] = expectedTransactionId }
            };
            
            // Act
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.NotEmpty(correlationInfo.CycleId);
            Assert.Equal(expectedOperationId, correlationInfo.OperationId);
            Assert.Equal(expectedTransactionId, correlationInfo.TransactionId);
        }

        [Fact]
        public void GetMessageCorrelationInfo_WithTransactionIdAsUserProperty_ReturnsCorrelationInfoWithGeneratedOperationId()
        {
            // Arrange
            string expectedTransactionId = $"transaction-{Guid.NewGuid()}";
            var message = new Message
            {
                UserProperties = { [PropertyNames.TransactionId] = expectedTransactionId }
            };

            // Act
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.NotEmpty(correlationInfo.CycleId);
            Assert.NotEmpty(correlationInfo.OperationId);
            Assert.Equal(expectedTransactionId, correlationInfo.TransactionId);
        }

        [Fact]
        public void GetMessageCorrelationInfo_WithTransactionIdInCustomUserProperty_ReturnsCorrelationInfoWithGeneratedOperationId()
        {
            // Arrange
            string expectedTransactionId = $"transaction-{Guid.NewGuid()}";
            const string transactionIdPropertyName = "Correlation-Transaction-Id";
            var message = new Message
            {
                UserProperties = { [transactionIdPropertyName] = expectedTransactionId }
            };

            // Act
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo(transactionIdPropertyName: transactionIdPropertyName);

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.NotEmpty(correlationInfo.CycleId);
            Assert.NotEmpty(correlationInfo.OperationId);
            Assert.Equal(expectedTransactionId, correlationInfo.TransactionId);
        }

        [Fact]
        public void GetMessageCorrelationInfo_WithCorrelationId_ReturnsCorrelationInfoWithEmptyTransactionId()
        {
            // Arrange
            string expectedOperationId = $"operation-{Guid.NewGuid()}";
            var message = new Message { CorrelationId = expectedOperationId };
            
            // Act
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.NotEmpty(correlationInfo.CycleId);
            Assert.Equal(expectedOperationId, correlationInfo.OperationId);
            Assert.Empty(correlationInfo.TransactionId);
        }
    }
}
