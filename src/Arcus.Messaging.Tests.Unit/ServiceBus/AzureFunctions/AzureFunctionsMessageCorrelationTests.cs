using System;
using System.Collections.Generic;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.AzureFunctions.ServiceBus;
using Arcus.Messaging.Tests.Core.Correlation;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus.AzureFunctions
{
    public class AzureFunctionsMessageCorrelationTests
    {
        [Fact]
        public void Correlate_WithDefault_Succeeds()
        {
            // Arrange
            var client = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            var correlation = new AzureFunctionsMessageCorrelation(client);
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage();

            // Act
            using (MessageCorrelationResult result = correlation.CorrelateMessage(message))
            {
                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.CorrelationInfo);
                Assert.False(string.IsNullOrWhiteSpace(result.CorrelationInfo.OperationId));
                Assert.False(string.IsNullOrWhiteSpace(result.CorrelationInfo.TransactionId));
                Assert.False(string.IsNullOrWhiteSpace(result.CorrelationInfo.OperationParentId));
                Assert.False(string.IsNullOrWhiteSpace(result.CorrelationInfo.CycleId));
            }
        }

        [Fact]
        public void Correlate_WithCustom_Succeeds()
        {
            // Arrange
            var client = new TelemetryClient(TelemetryConfiguration.CreateDefault());
            var correlation = new AzureFunctionsMessageCorrelation(client);
            var traceParent = TraceParent.Generate();
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                properties: new Dictionary<string, object> { ["Diagnostic-Id"] = traceParent.DiagnosticId });

            // Act
            using (MessageCorrelationResult result = correlation.CorrelateMessage(message))
            {
                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.CorrelationInfo);
                Assert.False(string.IsNullOrWhiteSpace(result.CorrelationInfo.OperationId));
                Assert.Equal(traceParent.TransactionId, result.CorrelationInfo.TransactionId);
                Assert.Equal(traceParent.OperationParentId, result.CorrelationInfo.OperationParentId);
                Assert.False(string.IsNullOrWhiteSpace(result.CorrelationInfo.CycleId));
            }
        }
        
        [Fact]
        public void Create_WithoutTelemetryClient_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new AzureFunctionsMessageCorrelation(client: null));
        }
    }
}
