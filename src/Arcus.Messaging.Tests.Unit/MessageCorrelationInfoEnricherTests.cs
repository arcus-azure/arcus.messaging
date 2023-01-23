using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.Telemetry;
using Bogus;
using Bogus.Extensions;
using Moq;
using Serilog.Core;
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
            var factory = new Mock<ILogEventPropertyFactory>();
            factory.Setup(f => f.CreateProperty(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<bool>()))
                   .Returns<string, object>((name, value) => new LogEventProperty(name, new ScalarValue(value)));

            // Act
            enricher.Enrich(logEvent, factory.Object);

            // Assert
            Assert.Contains(logEvent.Properties,
                prop => prop.Key == options.OperationIdPropertyName &&
                        prop.Value.ToString() == correlationInfo.OperationId);
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
    }
}
