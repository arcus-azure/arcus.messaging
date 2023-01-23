using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Correlation;
using Bogus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit
{
    // ReSharper disable once InconsistentNaming
    public class IDictionaryExtensionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void GetTraceParent_WithBlankDiagnosticId_GeneratesNewCorrelationIds()
        {
            // Arrange
            IReadOnlyDictionary<string, object> properties =
                new ReadOnlyDictionary<string, object>(new Dictionary<string, object>
                {
                    ["Diagnostic-Id"] = ""
                });

            // Act
            (string transactionId, string operationParentId) = properties.GetTraceParent();

            // Assert
            Assert.Equal(32, transactionId.Length);
            Assert.Equal(16, operationParentId.Length);
        }

        [Fact]
        public void GetTraceParent_WithExtraPadding_GetsTrimmed()
        {
            // Arrange
            var traceParent = TraceParent.Generate();
            var noise = BogusGenerator.Random.String();
            IReadOnlyDictionary<string, object> properties =
                new ReadOnlyDictionary<string, object>(new Dictionary<string, object>
                {
                    ["Diagnostic-Id"] = $"{traceParent.DiagnosticId}{noise}"
                });

            // Act
            (string transactionId, string operationParentId) = properties.GetTraceParent();

            // Assert
            Assert.Equal(32, transactionId.Length);
            Assert.Equal(16, operationParentId.Length);
        }
    }
}
