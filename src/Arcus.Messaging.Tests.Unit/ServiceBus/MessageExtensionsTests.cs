using System;
using Arcus.Messaging.Abstractions;
using Microsoft.Azure.ServiceBus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    [Trait("Category", "Unit")]
    public class MessageExtensionsTests
    {
        [Fact]
        public void GetTransactionId_NoTransactionIdSpecified_ReturnsEmptyString()
        {
            // Arrange
            var serviceBusMessage = new Message();

            // Act
            var transactionId = serviceBusMessage.GetTransactionId();

            // Assert
            Assert.Empty(transactionId);
        }

        [Fact]
        public void GetTransactionId_TransactionIdSpecified_ReturnsCorrectTransactionId()
        {
            // Arrange
            var expectedTransactionId = Guid.NewGuid().ToString();
            var serviceBusMessage = new Message();
            serviceBusMessage.UserProperties.Add(PropertyNames.TransactionId, expectedTransactionId);

            // Act
            var transactionId = serviceBusMessage.GetTransactionId();

            // Assert
            Assert.Equal(expectedTransactionId,transactionId);
        }
    }
}
