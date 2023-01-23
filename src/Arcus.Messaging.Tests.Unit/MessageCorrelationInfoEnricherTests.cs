using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.Telemetry;
using Arcus.Messaging.Tests.Unit.Fixture;
using Arcus.Observability.Correlation;
using Bogus;
using Bogus.Extensions;
using Serilog.Events;
using Xunit;

namespace Arcus.Messaging.Tests.Unit
{
    public class MessageCorrelationInfoEnricherTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void Enrich_WithDefault_AddsCorrelationProperties()
        {
            // Arrange
            var correlationInfo = new MessageCorrelationInfo(
                $"operation-{Guid.NewGuid()}",
                $"transaction-{Guid.NewGuid()}",
                $"parent-{Guid.NewGuid()}");
            var options = new MessageCorrelationEnricherOptions();
            var enricher = new MessageCorrelationInfoEnricher(correlationInfo, options);

            LogEvent logEvent = GenerateLogEvent();
            var factory = new TestLogEventPropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            AssertLogProperty(logEvent.Properties, options.OperationIdPropertyName, correlationInfo.OperationId);
            AssertLogProperty(logEvent.Properties, options.TransactionIdPropertyName, correlationInfo.TransactionId);
            AssertLogProperty(logEvent.Properties, options.OperationParentIdPropertyName, correlationInfo.OperationParentId);
            AssertLogPropertyNotBlank(logEvent.Properties, options.CycleIdPropertyName);
        }

        [Fact]
        public void Enrich_WithCustomOperationIdPropertyName_Succeeds()
        {
            // Arrange
            var correlationInfo = new MessageCorrelationInfo(
                $"operation-{Guid.NewGuid()}",
                $"transaction-{Guid.NewGuid()}",
                $"parent-{Guid.NewGuid()}");
            string propertyName = $"OperationId-{Guid.NewGuid()}";
            var options = new MessageCorrelationEnricherOptions { OperationIdPropertyName = propertyName };
            var enricher = new MessageCorrelationInfoEnricher(correlationInfo, options);

            LogEvent logEvent = GenerateLogEvent();
            var factory = new TestLogEventPropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            AssertLogProperty(logEvent.Properties, propertyName, correlationInfo.OperationId);
            AssertLogProperty(logEvent.Properties, options.TransactionIdPropertyName, correlationInfo.TransactionId);
            AssertLogProperty(logEvent.Properties, options.OperationParentIdPropertyName, correlationInfo.OperationParentId);
            AssertLogPropertyNotBlank(logEvent.Properties, options.CycleIdPropertyName);
        }

        [Fact]
        public void Enrich_WithCustomTransactionIdPropertyName_Succeeds()
        {
            // Arrange
            var correlationInfo = new MessageCorrelationInfo(
                $"operation-{Guid.NewGuid()}",
                $"transaction-{Guid.NewGuid()}",
                $"parent-{Guid.NewGuid()}");
            string propertyName = $"TransactionId-{Guid.NewGuid()}";
            var options = new MessageCorrelationEnricherOptions { TransactionIdPropertyName = propertyName };
            var enricher = new MessageCorrelationInfoEnricher(correlationInfo, options);

            LogEvent logEvent = GenerateLogEvent();
            var factory = new TestLogEventPropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            AssertLogProperty(logEvent.Properties, options.OperationIdPropertyName, correlationInfo.OperationId);
            AssertLogProperty(logEvent.Properties, propertyName, correlationInfo.TransactionId);
            AssertLogProperty(logEvent.Properties, options.OperationParentIdPropertyName, correlationInfo.OperationParentId);
            AssertLogPropertyNotBlank(logEvent.Properties, options.CycleIdPropertyName);
        }

        [Fact]
        public void Enrich_WithCustomOperationParentIdPropertyName_Succeeds()
        {
            // Arrange
            var correlationInfo = new MessageCorrelationInfo(
                $"operation-{Guid.NewGuid()}",
                $"transaction-{Guid.NewGuid()}",
                $"parent-{Guid.NewGuid()}");
            string propertyName = $"OperationParentId-{Guid.NewGuid()}";
            var options = new MessageCorrelationEnricherOptions { OperationParentIdPropertyName = propertyName };
            var enricher = new MessageCorrelationInfoEnricher(correlationInfo, options);

            LogEvent logEvent = GenerateLogEvent();
            var factory = new TestLogEventPropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            AssertLogProperty(logEvent.Properties, options.OperationIdPropertyName, correlationInfo.OperationId);
            AssertLogProperty(logEvent.Properties, options.TransactionIdPropertyName, correlationInfo.TransactionId);
            AssertLogProperty(logEvent.Properties, propertyName, correlationInfo.OperationParentId);
            AssertLogPropertyNotBlank(logEvent.Properties, options.CycleIdPropertyName);
        }

        [Fact]
        public void Enrich_WithCustomCycleIdPropertyName_Succeeds()
        {
            // Arrange
            var correlationInfo = new MessageCorrelationInfo(
                $"operation-{Guid.NewGuid()}",
                $"transaction-{Guid.NewGuid()}",
                $"parent-{Guid.NewGuid()}");
            string propertyName = $"CycleId-{Guid.NewGuid()}";
            var options = new MessageCorrelationEnricherOptions { CycleIdPropertyName = propertyName };
            var enricher = new MessageCorrelationInfoEnricher(correlationInfo, options);

            LogEvent logEvent = GenerateLogEvent();
            var factory = new TestLogEventPropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            AssertLogProperty(logEvent.Properties, options.OperationIdPropertyName, correlationInfo.OperationId);
            AssertLogProperty(logEvent.Properties, options.TransactionIdPropertyName, correlationInfo.TransactionId);
            AssertLogProperty(logEvent.Properties, options.OperationParentIdPropertyName, correlationInfo.OperationParentId);
            AssertLogPropertyNotBlank(logEvent.Properties, propertyName);
        }

        private static LogEvent GenerateLogEvent()
        {
            return new LogEvent(
                BogusGenerator.Date.RecentOffset(),
                BogusGenerator.PickRandom<LogEventLevel>(),
                BogusGenerator.System.Exception().OrNull(BogusGenerator),
                MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());
        }

        private static void AssertLogProperty(IReadOnlyDictionary<string, LogEventPropertyValue> properties, string key, string value)
        {
            Assert.Contains(properties, prop => prop.Key == key && prop.Value.ToDecentString() == value);
        }

        private static void AssertLogPropertyNotBlank(IReadOnlyDictionary<string, LogEventPropertyValue> properties, string key)
        {
            Assert.Contains(properties, prop => prop.Key == key && !string.IsNullOrWhiteSpace(prop.Value.ToDecentString()));
        }

        [Fact]
        public void Enrich_WithoutCorrelationInfo_EarlyReturn()
        {
            // Arrange
            var accessor = new DefaultCorrelationInfoAccessor<MessageCorrelationInfo>();
            var enricher = new MessageCorrelationInfoEnricher(accessor);
            LogEvent logEvent = GenerateLogEvent();
            var factory = new TestLogEventPropertyFactory();

            // Act
            enricher.Enrich(logEvent, factory);

            // Assert
            Assert.Empty(logEvent.Properties);
        }
    }
}
