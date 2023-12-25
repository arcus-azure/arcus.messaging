using System;
using System.Collections.Generic;
using Arcus.Messaging.Tests.Core.Correlation;
using Xunit;

namespace Arcus.Messaging.Tests.Integration.Correlation
{
    // ReSharper disable once InconsistentNaming
    public class IDictionaryExtensionsTests
    {
        [Fact]
        public void Properties_WithValidDiagnosticId_Generates()
        {
            // Arrange
            var traceParent = TraceParent.Generate();
            IDictionary<string, object> properties = new Dictionary<string, object>
            {
                ["Diagnostic-Id"] = traceParent.DiagnosticId
            };

            // Act
            (string transactionId, string operationParentId) = properties.GetTraceParent();

            // Assert
            Assert.Equal(traceParent.TransactionId, transactionId);
            Assert.Equal(traceParent.OperationParentId, operationParentId);
        }

        [Fact]
        public void Properties_WithoutDiagnosticId_Generates()
        {
            // Arrange
            IDictionary<string, object> properties = new Dictionary<string, object>();

            // Act
            (string transactionId, string operationParentId) = properties.GetTraceParent();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(transactionId));
            Assert.False(string.IsNullOrWhiteSpace(operationParentId));
        }

        [Fact]
        public void Properties_WithInvalidDiagnosticId_Generates()
        {
            // Arrange
            IDictionary<string, object> properties = new Dictionary<string, object>
            {
                ["Diagnostic-Id"] = "Something not even remotely a correct 'traceparent'"
            };

            // Act
            (string transactionId, string operationParentId) = properties.GetTraceParent();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(transactionId));
            Assert.False(string.IsNullOrWhiteSpace(operationParentId));
        }
    }
}
