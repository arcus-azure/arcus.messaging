using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Bogus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessageHandling
{
    public class MessageResultTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void CreateFailure_WithException_Succeeds()
        {
            // Arrange
            Exception exception = BogusGenerator.System.Exception();

            // Act
            var result = MessageResult.Failure(exception);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Same(exception, result.Exception);
        }

        [Fact]
        public void CreateFailure_WithoutException_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => MessageResult.Failure(exception: null));
        }
    }
}
