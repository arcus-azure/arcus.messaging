using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    public class MessageTelemetryOptionsTests
    {
        [Fact]
        public void Set_ValidOperation_Succeeds()
        {
            // Arrange
            var options = new MessageTelemetryOptions();
            string operationName = Guid.NewGuid().ToString();

            // Act
            options.OperationName = operationName;

            // Assert
            Assert.Equal(operationName, options.OperationName);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void Set_BlankOperationName_Fails(string operationName)
        {
            // Arrange
            var options = new MessageTelemetryOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.OperationName = operationName);
        }
    }
}
