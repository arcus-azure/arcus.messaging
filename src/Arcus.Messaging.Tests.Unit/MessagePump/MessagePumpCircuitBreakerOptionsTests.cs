using System;
using Arcus.Messaging.Pumps.Abstractions.Transient;
using Bogus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessagePump
{
    public class MessagePumpCircuitBreakerOptionsTests
    {
        private static readonly Faker Bogus = new();

        [Fact]
        public void Default_MessageRecoveryPeriod_Initialized()
        {
            // Arrange
            var options = new MessagePumpCircuitBreakerOptions();

            // Act
            TimeSpan period = options.MessageRecoveryPeriod;

            // Assert
            Assert.True(period > TimeSpan.Zero, "Default message recovery period should be greater than a zero time period");
        }

        [Fact]
        public void SetMessageRecoveryPeriod_WithPositiveTimePeriod_Succeeds()
        {
            // Arrange
            var period = Bogus.Date.Timespan();
            var options = new MessagePumpCircuitBreakerOptions();

            // Act
            options.MessageRecoveryPeriod = period;

            // Assert
            Assert.Equal(period, options.MessageRecoveryPeriod);
        }

        [Fact]
        public void SetMessageRecoveryPeriod_WithNegativeTimePeriod_Fails()
        {
            // Arrange
            var period = Bogus.Date.Timespan(maxSpan: TimeSpan.Zero);
            var options = new MessagePumpCircuitBreakerOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.MessageRecoveryPeriod = period);
        }

        [Fact]
        public void Default_MessageIntervalPeriod_Initialized()
        {
            // Arrange
            var options = new MessagePumpCircuitBreakerOptions();

            // Act
            TimeSpan period = options.MessageIntervalDuringRecovery;

            // Assert
            Assert.True(period > TimeSpan.Zero, "Default message interval period should be greater than a zero time period");
        }

        [Fact]
        public void SetMessageIntervalPeriod_WithPositiveTimePeriod_Succeeds()
        {
            // Arrange
            var period = Bogus.Date.Timespan();
            var options = new MessagePumpCircuitBreakerOptions();

            // Act
            options.MessageIntervalDuringRecovery = period;

            // Assert
            Assert.Equal(period, options.MessageIntervalDuringRecovery);
        }

        [Fact]
        public void SetMessageIntervalPeriod_WithNegativeTimePeriod_Fails()
        {
            // Arrange
            var period = Bogus.Date.Timespan(maxSpan: TimeSpan.Zero);
            var options = new MessagePumpCircuitBreakerOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.MessageIntervalDuringRecovery = period);
        }
    }
}
