using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Correlation;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.EventHubs.AzureFunctions
{
    public class FunctionContextExtensions
    {
        [Fact]
        public void GetCorrelationInfoForW3C_WithoutTraceParent_GetsNewParent()
        {
            // Arrange
            var properties = new Dictionary<string, JsonElement>();
            FunctionContext context = CreateFunctionContext(
                configureServices: services => services.AddSingleton<TelemetryClient>());

            // Act
            using (MessageCorrelationResult result = context.GetCorrelationInfo(properties))
            {
                // Assert
                Assert.NotNull(result.CorrelationInfo);
                Assert.NotNull(result.CorrelationInfo.OperationId);
                Assert.NotNull(result.CorrelationInfo.TransactionId);
                Assert.NotNull(result.CorrelationInfo.OperationParentId);
            }
        }

        [Fact]
        public void GetCorrelationInfoForW3C_WithTraceParent_GetsExistingParent()
        {
            // Arrange
            var traceParent = TraceParent.Generate();
            var properties = new Dictionary<string, JsonElement>
            {
                ["Diagnostic-Id"] = JsonSerializer.SerializeToElement(traceParent.DiagnosticId)
            };
            FunctionContext context = CreateFunctionContext(
                configureServices: services => services.AddSingleton<TelemetryClient>());

            // Act
            using (MessageCorrelationResult result = context.GetCorrelationInfo(properties))
            {
                // Assert
                Assert.NotNull(result.CorrelationInfo);
                Assert.NotNull(result.CorrelationInfo.OperationId);
                Assert.Equal(traceParent.TransactionId, result.CorrelationInfo.TransactionId);
                Assert.Equal(traceParent.OperationParentId, result.CorrelationInfo.OperationParentId);
            }
        }

        [Fact]
        public void GetCorrelationInfoForHierarchical_WithoutCorrelationProperties_GetsNewValues()
        {
            // Arrange
            string operationId = Guid.NewGuid().ToString();
            var properties = new Dictionary<string, JsonElement>
            {
                ["CorrelationId"] = JsonSerializer.SerializeToElement(operationId)
            };
            FunctionContext context = CreateFunctionContext(
                configureServices: services => services.AddSingleton<TelemetryClient>());

            // Act
            using (MessageCorrelationResult result =
                   context.GetCorrelationInfo(properties, MessageCorrelationFormat.Hierarchical))
            {
                // Assert
                Assert.NotNull(result.CorrelationInfo);
                Assert.Equal(operationId, result.CorrelationInfo.OperationId);
                Assert.NotNull(result.CorrelationInfo.TransactionId);
                Assert.Null(result.CorrelationInfo.OperationParentId);
            }
        }

        [Fact]
        public void GetCorrelationInfoForHierarchical_WithTransactionId_GetTransactionId()
        {
            // Arrange
            string operationId = Guid.NewGuid().ToString();
            string transactionId = Guid.NewGuid().ToString();
            var properties = new Dictionary<string, JsonElement>
            {
                ["CorrelationId"] = JsonSerializer.SerializeToElement(operationId),
                [PropertyNames.TransactionId] = JsonSerializer.SerializeToElement(transactionId)
            };
            FunctionContext context = CreateFunctionContext(
                configureServices: services => services.AddSingleton<TelemetryClient>());

            // Act
            using (MessageCorrelationResult result =
                   context.GetCorrelationInfo(properties, MessageCorrelationFormat.Hierarchical))
            {
                // Assert
                Assert.NotNull(result.CorrelationInfo);
                Assert.Equal(operationId, result.CorrelationInfo.OperationId);
                Assert.Equal(transactionId, result.CorrelationInfo.TransactionId);
                Assert.Null(result.CorrelationInfo.OperationParentId);
            }
        }

        [Fact]
        public void GetCorrelationInfoForHierarchical_WithOperationParentId_GetOperationParentId()
        {
            // Arrange
            string operationId = Guid.NewGuid().ToString();
            string operationParentId = Guid.NewGuid().ToString();
            var properties = new Dictionary<string, JsonElement>
            {
                ["CorrelationId"] = JsonSerializer.SerializeToElement(operationId),
                [PropertyNames.OperationParentId] = JsonSerializer.SerializeToElement(operationParentId)
            };
            FunctionContext context = CreateFunctionContext(
                configureServices: services => services.AddSingleton<TelemetryClient>());

            // Act
            using (MessageCorrelationResult result =
                   context.GetCorrelationInfo(properties, MessageCorrelationFormat.Hierarchical))
            {
                // Assert
                Assert.NotNull(result.CorrelationInfo);
                Assert.Equal(operationId, result.CorrelationInfo.OperationId);
                Assert.NotNull(result.CorrelationInfo.TransactionId);
                Assert.Equal(operationParentId, result.CorrelationInfo.OperationParentId);
            }
        }

        [Fact]
        public void GetCorrelationInfoForHierarchical_WithOperationParentIdAndTransactionId_GetOperationParentIdAndTransactionId()
        {
            // Arrange
            string operationId = Guid.NewGuid().ToString();
            string transactionId = Guid.NewGuid().ToString();
            string operationParentId = Guid.NewGuid().ToString();
            var properties = new Dictionary<string, JsonElement>
            {
                ["CorrelationId"] = JsonSerializer.SerializeToElement(operationId),
                [PropertyNames.TransactionId] = JsonSerializer.SerializeToElement(transactionId),
                [PropertyNames.OperationParentId] = JsonSerializer.SerializeToElement(operationParentId)
            };
            FunctionContext context = CreateFunctionContext(
                configureServices: services => services.AddSingleton<TelemetryClient>());

            // Act
            using (MessageCorrelationResult result =
                   context.GetCorrelationInfo(properties, MessageCorrelationFormat.Hierarchical))
            {
                // Assert
                Assert.NotNull(result.CorrelationInfo);
                Assert.Equal(operationId, result.CorrelationInfo.OperationId);
                Assert.Equal(transactionId, result.CorrelationInfo.TransactionId);
                Assert.Equal(operationParentId, result.CorrelationInfo.OperationParentId);
            }
        }

        [Fact]
        public void GetCorrelationInfo_WithoutCorrelationId_GeneratesOperationId()
        {
            // Arrange
            var properties = new Dictionary<string, JsonElement>();
            FunctionContext context = CreateFunctionContext(
                configureServices: services => services.AddSingleton<TelemetryClient>());

            // Act
            using (MessageCorrelationResult result =
                   context.GetCorrelationInfo(properties, MessageCorrelationFormat.Hierarchical))
            {
                // Assert
                Assert.NotNull(result.CorrelationInfo);
                Assert.NotNull(result.CorrelationInfo.OperationId);
                Assert.NotNull(result.CorrelationInfo.TransactionId);
                Assert.Null(result.CorrelationInfo.OperationParentId);
            }
        }

        private FunctionContext CreateFunctionContext(
            IDictionary<string, object> bindingData = null,
            Action<IServiceCollection> configureServices = null)
        {
            var stubBindingContext = new Mock<BindingContext>();
            stubBindingContext.Setup(s => s.BindingData).Returns(new ReadOnlyDictionary<string, object>(bindingData ?? new Dictionary<string, object>()));

            var stubFuncContext = new Mock<FunctionContext>();
            stubFuncContext.Setup(s => s.BindingContext).Returns(stubBindingContext.Object);

            var services = new ServiceCollection();
            configureServices?.Invoke(services);
            stubFuncContext.Setup(s => s.InstanceServices).Returns(services.BuildServiceProvider());

            return stubFuncContext.Object;
        }
    }
}
