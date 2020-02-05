using System;
using Arcus.Messaging.Abstractions;
using Xunit;

namespace Arcus.Messaging.Tests.Unit
{
    [Trait("Category", "Unit")]
    public class MessageCorrelationInfoTests
    {
        [Fact]
        public void Constructor_Valid_Succeeds()
        {
            // Arrange
            var transactionId = Guid.NewGuid().ToString();
            var operationId = Guid.NewGuid().ToString();

            // Act
            var messageCorrelationInfo = new MessageCorrelationInfo(operationId, transactionId);

            // Assert
            Assert.Equal(operationId, messageCorrelationInfo.OperationId);
            Assert.Equal(transactionId, messageCorrelationInfo.TransactionId);
            Assert.NotEmpty(messageCorrelationInfo.CycleId);
        }

        [Fact]
        public void Constructor_NoTransactionIdSpecified_Succeeds()
        {
            // Arrange
            var operationId = Guid.NewGuid().ToString();

            // Act
            var messageCorrelationInfo = new MessageCorrelationInfo(operationId, transactionId: null);

            // Assert
            Assert.Equal(operationId, messageCorrelationInfo.OperationId);
            Assert.NotEmpty(messageCorrelationInfo.CycleId);
            Assert.Null(messageCorrelationInfo.TransactionId);
        }

        [Fact]
        public void Constructor_NoOperationIdSpecified_ThrowsException()
        {
            // Arrange
            var transactionId = Guid.NewGuid().ToString();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MessageCorrelationInfo(operationId: null, transactionId));
        }
    }
}
