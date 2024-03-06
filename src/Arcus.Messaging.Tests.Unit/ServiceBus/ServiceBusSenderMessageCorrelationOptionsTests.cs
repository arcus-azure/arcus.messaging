using System;
using Arcus.Messaging.ServiceBus.Core;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    public class ServiceBusSenderMessageCorrelationOptionsTests
    {
        [Fact]
        public void TransactionIdPropertyName_WithNoAction_UsesDefault()
        {
            // Arrange
            var options = new ServiceBusSenderMessageCorrelationOptions();

            // Act
            string headerName = options.TransactionIdPropertyName;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(headerName));
        }

        [Fact]
        public void TransactionIdPropertyName_SetValue_UsesValue()
        {
            // Arrange
            var options = new ServiceBusSenderMessageCorrelationOptions();
            string PropertyName = Guid.NewGuid().ToString();

            // Act
            options.TransactionIdPropertyName = PropertyName;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(PropertyName));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void TransactionIdPropertyName_WithoutValue_Fails(string PropertyName)
        {
            // Arrange
            var options = new ServiceBusSenderMessageCorrelationOptions();

            // ACt / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.TransactionIdPropertyName = PropertyName);
        }

        [Fact]
        public void UpstreamServicePropertyName_WithNoAction_UsesDefault()
        {
            // Arrange
            var options = new ServiceBusSenderMessageCorrelationOptions();

            // Act
            string headerName = options.UpstreamServicePropertyName;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(headerName));
        }

        [Fact]
        public void UpstreamServicePropertyName_SetValue_UsesValue()
        {
            // Arrange
            var options = new ServiceBusSenderMessageCorrelationOptions();
            string PropertyName = Guid.NewGuid().ToString();

            // Act
            options.UpstreamServicePropertyName = PropertyName;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(PropertyName));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UpstreamServicePropertyName_WithoutValue_Fails(string PropertyName)
        {
            // Arrange
            var options = new ServiceBusSenderMessageCorrelationOptions();

            // ACt / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.UpstreamServicePropertyName = PropertyName);
        }

        [Fact]
        public void GenerateDependencyId_WithoutAction_GeneratesDefault()
        {
            // Arrange
            var options = new ServiceBusSenderMessageCorrelationOptions();

            // Act
            string dependencyId = options.GenerateDependencyId();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(dependencyId));
        }

        [Fact]
        public void GenerateDependencyId_WithValue_UsesValue()
        {
            // Arrange
            var options = new ServiceBusSenderMessageCorrelationOptions();
            string dependencyId = Guid.NewGuid().ToString();

            // Act
            options.GenerateDependencyId = () => dependencyId;

            // Assert
            Assert.Equal(dependencyId, options.GenerateDependencyId());
        }

        [Fact]
        public void GenerateDependencyId_WithoutValue_Fails()
        {
            // Arrange
            var options = new ServiceBusSenderMessageCorrelationOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.GenerateDependencyId = null);
        }

        [Fact]
        public void AddTelemetryContext_WithoutValue_Fails()
        {
            // Arrange
            var options = new ServiceBusSenderMessageCorrelationOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.AddTelemetryContext(telemetryContext: null));
        }
    }
}
