using System;
using Arcus.Messaging.EventHubs.Core;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.EventHubs
{
    public class EventHubProducerClientMessageCorrelationOptionsTests
    {
        [Fact]
        public void TransactionIdPropertyName_WithoutAction_SetsDefault()
        {
            // Arrange
            var options = new EventHubProducerClientMessageCorrelationOptions();

            // Act
            string propertyName = options.TransactionIdPropertyName;
            
            // Assert
            Assert.NotNull(propertyName);
            Assert.NotEmpty(propertyName);
        }

        [Fact]
        public void TransactionIdPropertyName_SetValue_Succeeds()
        {
            // Arrange
            var propertyName = "My-Transaction-Id";
            var options = new EventHubProducerClientMessageCorrelationOptions();

            // Act
            options.TransactionIdPropertyName = propertyName;

            // Assert
            Assert.Equal(propertyName, options.TransactionIdPropertyName);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void TransactionIdPropertyName_SetWithoutValue_Fails(string propertyName)
        {
            // Arrange
            var options = new EventHubProducerClientMessageCorrelationOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.TransactionIdPropertyName = propertyName);
        }

        [Fact]
        public void UpstreamServicePropertyName_WithoutAction_SetsDefault()
        {
            // Arrange
            var options = new EventHubProducerClientMessageCorrelationOptions();

            // Act
            string propertyName = options.UpstreamServicePropertyName;

            // Assert
            Assert.NotNull(propertyName);
            Assert.NotEmpty(propertyName);
        }

        [Fact]
        public void UpstreamServiceIdPropertyName_SetValue_Succeeds()
        {
            // Arrange
            var propertyName = "My-UpstreamService-Id";
            var options = new EventHubProducerClientMessageCorrelationOptions();

            // Act
            options.UpstreamServicePropertyName = propertyName;

            // Assert
            Assert.Equal(propertyName, options.UpstreamServicePropertyName);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UpstreamServiceIdPropertyName_SetWithoutValue_Fails(string propertyName)
        {
            // Arrange
            var options = new EventHubProducerClientMessageCorrelationOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.UpstreamServicePropertyName = propertyName);
        }

        [Fact]
        public void GenerateDependencyId_WithCustomGeneration_Succeeds()
        {
            // Arrange
            var dependencyId = Guid.NewGuid().ToString();
            var options = new EventHubProducerClientMessageCorrelationOptions();

            // Act
            options.GenerateDependencyId = () => dependencyId;

            // Assert
            Assert.Equal(dependencyId, options.GenerateDependencyId());
        }

        [Fact]
        public void GenerateDependencyId_WithoutCustomGeneration_Fails()
        {
            // Arrange
            var options = new EventHubProducerClientMessageCorrelationOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.GenerateDependencyId = null);
        }

        [Fact]
        public void GenerateDependencyId_WithoutAction_GeneratedDefault()
        {
            // Arrange
            var options = new EventHubProducerClientMessageCorrelationOptions();

            // Act
            string dependencyId = options.GenerateDependencyId();

            // Assert
            Assert.NotNull(dependencyId);
            Assert.NotEmpty(dependencyId);
        }
    }
}
