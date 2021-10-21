using System;
using Arcus.Messaging.Abstractions;
using Arcus.Observability.Correlation;
using Xunit;

namespace Arcus.Messaging.Tests.Unit
{
    public class MessageCorrelationInfoAccessorTests
    {
        [Fact]
        public void Create_WithoutCorrelationInfoAccessorImplementation_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new MessageCorrelationInfoAccessor(implementation: null));
        }

        [Fact]
        public void Create_WithStubbedCorrelationInfoAccessor_UsesImplementation()
        {
            // Arrange
            var correlation = new MessageCorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}");
            var implementation = new DefaultCorrelationInfoAccessor<MessageCorrelationInfo>();

            var accessor = new MessageCorrelationInfoAccessor(implementation);

            // Act
            accessor.SetCorrelationInfo(correlation);

            // Assert
            Assert.Equal(correlation, accessor.GetCorrelationInfo());
            Assert.Equal(correlation, implementation.GetCorrelationInfo());
        }
    }
}
