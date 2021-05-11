using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.ServiceBus;
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
            ServiceBusReceivedMessage message = CreateMessage(key, new Uri("http://localhost"));

            // Act
            var uri = message.GetApplicationProperty<Uri>(key);

            // Assert
            Assert.NotNull(uri);
        }

        [Fact]
        public void GetUserProperty_WithNonExistingKey_ThrowsKeyNotFound()
        {
            // Arrange
            ServiceBusReceivedMessage serviceBusMessage = CreateMessage();

            // Act / Assert
            Assert.Throws<KeyNotFoundException>(
                () => serviceBusMessage.GetApplicationProperty<Uri>("non-existing-key"));
        }

        [Fact]
        public void GetUserProperty_WithWrongUserPropertyValue_ThrowsInvalidCast()
        {
            // Arrange
            const string key = "uri-key";
            ServiceBusReceivedMessage serviceBusMessage = CreateMessage(key, TimeSpan.Zero);

            // Act / Assert
            Assert.Throws<InvalidCastException>(
                () => serviceBusMessage.GetApplicationProperty<Uri>(key));
        }

        [Fact]
        public void GetMessageCorrelationInfo_WithCorrelationIdAndTransactionIdAsUserProperties_ReturnsCorrelationInfo()
        {
            // Arrange
            var expectedOperationId = $"operation-{Guid.NewGuid()}";
            var expectedTransactionId = $"transaction-{Guid.NewGuid()}";
            ServiceBusReceivedMessage message = CreateMessage(PropertyNames.TransactionId, expectedTransactionId, expectedOperationId);
            
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
            var expectedTransactionId = $"transaction-{Guid.NewGuid()}";
            ServiceBusReceivedMessage message = CreateMessage(PropertyNames.TransactionId, expectedTransactionId);

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
            var expectedTransactionId = $"transaction-{Guid.NewGuid()}";
            const string transactionIdPropertyName = "Correlation-Transaction-Id";
            ServiceBusReceivedMessage message = CreateMessage(transactionIdPropertyName, expectedTransactionId);

            // Act
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo(transactionIdPropertyName: transactionIdPropertyName);

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.NotEmpty(correlationInfo.CycleId);
            Assert.NotEmpty(correlationInfo.OperationId);
            Assert.Equal(expectedTransactionId, correlationInfo.TransactionId);
        }

        [Fact]
        public void GetMessageCorrelationInfo_WithCorrelationId_ReturnsCorrelationInfoWithNonEmptyTransactionId()
        {
            // Arrange
            var expectedOperationId = $"operation-{Guid.NewGuid()}";
            ServiceBusReceivedMessage message = CreateMessage(correlationId: expectedOperationId);

            // Act
            MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.NotEmpty(correlationInfo.CycleId);
            Assert.Equal(expectedOperationId, correlationInfo.OperationId);
            Assert.NotEmpty(correlationInfo.TransactionId);
        }

        [Fact]
        public void GetTransactionId_WithoutTransactionIdAsUserProperty_ReturnsEmptyString()
        {
            // Arrange
            ServiceBusReceivedMessage message = CreateMessage(key: null, value: null);

            // Act
            string transactionId = message.GetTransactionId();

            // Assert
            Assert.Null(transactionId);
        }

        [Fact]
        public void GetTransactionId_WithTransactionIdAsUserProperty_ReturnsCorrectTransactionId()
        {
            // Arrange
            var expectedTransactionId = $"transaction-{Guid.NewGuid()}";
            ServiceBusReceivedMessage message = CreateMessage(PropertyNames.TransactionId, expectedTransactionId);

            // Act
            string transactionId = message.GetTransactionId();

            // Assert
            Assert.NotNull(transactionId);
            Assert.Equal(expectedTransactionId, transactionId);
        }

        [Fact]
        public void GetTransactionId_WithTransactionIdInCustomUserProperty_ReturnsCorrectTransactionId()
        {
            // Arrange
            var expectedTransactionId = $"transaction-{Guid.NewGuid()}";
            const string transactionIdPropertyName = "Correlation-Transaction-Id";
            ServiceBusReceivedMessage message = CreateMessage(transactionIdPropertyName, expectedTransactionId);

            // Act
            string transactionId = message.GetTransactionId(transactionIdPropertyName: transactionIdPropertyName);

            // Assert
            Assert.NotNull(transactionId);
            Assert.Equal(expectedTransactionId, transactionId);
        }

        private static ServiceBusReceivedMessage CreateMessage(string key = null, object value = null, string correlationId = null)
        {
            Order order = OrderGenerator.Generate();
            ServiceBusReceivedMessage message = order.AsServiceBusReceivedMessage(key, value, correlationId);

            return message;
        }
    }
}
