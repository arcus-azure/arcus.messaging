using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    [Trait("Category", "Unit")]
    public class MessageCorrelationOptionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void Set_BlankTransactionIdPropertyName_Fails(string transactionIdPropertyName)
        {
            // Arrange
            var options = new MessageCorrelationOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.TransactionIdPropertyName = transactionIdPropertyName);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void Set_BlankOperationParentIdPropertyName_Fails(string operationParentIdPropertyName)
        {
            // Arrange
            var options = new MessageCorrelationOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OperationParentIdPropertyName = operationParentIdPropertyName);
        }
    }
}
