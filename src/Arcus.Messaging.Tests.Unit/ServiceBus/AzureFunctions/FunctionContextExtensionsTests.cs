using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Correlation;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus.AzureFunctions
{
    public class FunctionContextExtensionsTests
    {
        [Fact]
        public void GetCorrelationInfo_WithDiagnosticId_Succeeds()
        {
            // Arrange
            var traceParent = TraceParent.Generate();
            FunctionContext context = CreateFunctionContext(
                CreateBindingDataWithUserProperties(new Dictionary<string, string> { ["Diagnostic-Id"] = traceParent.DiagnosticId }),
                services => services.AddSingleton<TelemetryClient>());

            // Act
            using (MessageCorrelationResult result = context.GetCorrelationInfo())
            {
                // Assert
                Assert.Equal(traceParent.TransactionId, result.CorrelationInfo.TransactionId);
                Assert.Equal(traceParent.OperationParentId, result.CorrelationInfo.OperationParentId);
            }
        }

        [Fact]
        public void GetCorrelationInfo_WithCustomCorrelationProperties_Succeeds()
        {
            // Arrange
            var transactionId = Guid.NewGuid().ToString();
            string operationParentId = Guid.NewGuid().ToString();
            FunctionContext context = CreateFunctionContext(
                CreateBindingDataWithUserProperties(new Dictionary<string, string>
                {
                    [PropertyNames.TransactionId] = transactionId,
                    [PropertyNames.OperationParentId] = operationParentId
                }));

            // Act
            using (MessageCorrelationResult result = context.GetCorrelationInfo(MessageCorrelationFormat.Hierarchical))
            {
                // Assert
                Assert.Equal(transactionId, result.CorrelationInfo.TransactionId);
                Assert.Equal(operationParentId, result.CorrelationInfo.OperationParentId);
            }
        }

        [Theory]
        [InlineData("My-Custom-Transaction", PropertyNames.OperationParentId)]
        [InlineData(PropertyNames.TransactionId, "My-Custom-Parent")]
        [InlineData("My-Custom-Transaction", "My-Custom-Parent")]
        public void GetCorrelationInfo_WithCustomCorrelationPropertyNames_Succeeds(
            string transactionIdPropertyName,
            string operationParentIdPropertyName)
        {
            // Arrange
            var transactionId = Guid.NewGuid().ToString();
            string operationParentId = Guid.NewGuid().ToString();
            FunctionContext context = CreateFunctionContext(
                CreateBindingDataWithUserProperties(new Dictionary<string, string>
                {
                    [transactionIdPropertyName] = transactionId,
                    [operationParentIdPropertyName] = operationParentId
                }));

            // Act
            using (MessageCorrelationResult result = context.GetCorrelationInfo(transactionIdPropertyName, operationParentIdPropertyName))
            {
                // Assert
                Assert.Equal(transactionId, result.CorrelationInfo.TransactionId);
                Assert.Equal(operationParentId, result.CorrelationInfo.OperationParentId);
            }
        }

        private static Dictionary<string, object> CreateBindingDataWithUserProperties(Dictionary<string, string> userProperties)
        {
            return new Dictionary<string, object>
            {
                { "UserProperties", JsonConvert.SerializeObject(userProperties) }
            };
        }

        [Fact]
        public void GetCorrelationInfo_WithoutUserProperties_Fails()
        {
            // Arrange
            FunctionContext context = CreateFunctionContext(bindingData: new Dictionary<string, object>());

            // Act / Assert
            Assert.ThrowsAny<InvalidOperationException>(() => context.GetCorrelationInfo());
        }

        [Fact]
        public void GetCorrelationInfo_WithoutBindingData_Fails()
        {
            // Arrange
            FunctionContext context = CreateFunctionContext(bindingData: null);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => context.GetCorrelationInfo());
        }

        [Theory]
        [InlineData(MessageCorrelationFormat.Hierarchical)]
        [InlineData(MessageCorrelationFormat.W3C)]
        public void GetCorrelationInfoWithFormat_WithoutUserProperties_Fails(MessageCorrelationFormat format)
        {
            // Arrange
            FunctionContext context = CreateFunctionContext(bindingData: new Dictionary<string, object>());

            // Act / Assert
            Assert.ThrowsAny<InvalidOperationException>(() => context.GetCorrelationInfo(format));
        }

        [Theory]
        [InlineData(MessageCorrelationFormat.Hierarchical)]
        [InlineData(MessageCorrelationFormat.W3C)]
        public void GetCorrelationInfoWithFormat_WithoutBindingData_Fails(MessageCorrelationFormat format)
        {
            // Arrange
            FunctionContext context = CreateFunctionContext(bindingData: null);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => context.GetCorrelationInfo(format));
        }

        private FunctionContext CreateFunctionContext(
            IDictionary<string, object> bindingData,
            Action<IServiceCollection> configureServices = null)
        {
            var stubBindingContext = new Mock<BindingContext>();
            stubBindingContext.Setup(s => s.BindingData).Returns(new ReadOnlyDictionary<string, object>(bindingData));

            var stubFuncContext = new Mock<FunctionContext>();
            stubFuncContext.Setup(s => s.BindingContext).Returns(stubBindingContext.Object);

            var services = new ServiceCollection();
            configureServices?.Invoke(services);
            stubFuncContext.Setup(s => s.InstanceServices).Returns(services.BuildServiceProvider());

            return stubFuncContext.Object;
        }
    }
}
