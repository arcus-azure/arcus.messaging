using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Unit.EventHubs.Fixture;
using Arcus.Testing;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Bogus;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.EventHubs
{
    public class EventHubProducerClientExtensionsTests
    {
        private const string DependencyIdPattern = @"with ID [a-z0-9]{8}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{12}";

        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithMessageCorrelation_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryEventHubProducerClient();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendAsync(expectedOrders, correlation, logger);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders),
                item => AssertEnrichedEventData(item.First, item.Second, correlation));
        }

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithCustomDependencyId_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryEventHubProducerClient();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var dependencyId = $"dependency-{Guid.NewGuid()}";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendAsync(expectedOrders, correlation, logger, options => options.GenerateDependencyId = () => dependencyId);

            // Assert
            AssertDependencyTelemetry(logger, dependencyId);
            Assert.All(
                spySender.Messages.Zip(expectedOrders),
                item => AssertEnrichedEventDataWithDependencyId(item.First, item.Second, correlation, dependencyId));
        }

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithCustomTransactionIdPropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryEventHubProducerClient();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var transactionIdPropertyName = "My-Transaction-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendAsync(expectedOrders, correlation, logger, options => options.TransactionIdPropertyName = transactionIdPropertyName);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders),
                item => AssertEnrichedEventData(item.First, item.Second, correlation, transactionIdPropertyName: transactionIdPropertyName));
        }

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithCustomUpstreamServiceIdPropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryEventHubProducerClient();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var upstreamServicePropertyName = "My-UpstreamService-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendAsync(expectedOrders, correlation, logger, options => options.UpstreamServicePropertyName = upstreamServicePropertyName);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders),
                item => AssertEnrichedEventData(item.First, item.Second, correlation, operationParentPropertyName: upstreamServicePropertyName));
        }

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithCustomTelemetryContext_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryEventHubProducerClient();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            string key = Guid.NewGuid().ToString(), value = Guid.NewGuid().ToString();
            var telemetryContext = new Dictionary<string, object> { [key] = value };
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendAsync(expectedOrders, correlation, logger, options => options.AddTelemetryContext(telemetryContext));

            // Assert
            string logMessage = AssertDependencyTelemetry(logger);
            Assert.Contains(key, logMessage);
            Assert.Contains(value, logMessage);
            Assert.All(
                spySender.Messages.Zip(expectedOrders),
                item => AssertEnrichedEventData(item.First, item.Second, correlation));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithMessageCorrelation_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryEventHubProducerClient();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var messages = expectedOrders.Select(order => EventDataBuilder.CreateForBody(order).Build());

            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendAsync(messages, correlation, logger);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders),
                item => AssertEnrichedEventData(item.First, item.Second, correlation));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithCustomDependencyId_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryEventHubProducerClient();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var messages = expectedOrders.Select(order => EventDataBuilder.CreateForBody(order).Build());

            var dependencyId = $"dependency-{Guid.NewGuid()}";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendAsync(messages, correlation, logger, options => options.GenerateDependencyId = () => dependencyId);

            // Assert
            AssertDependencyTelemetry(logger, dependencyId);
            Assert.All(
                spySender.Messages.Zip(expectedOrders),
                item => AssertEnrichedEventDataWithDependencyId(item.First, item.Second, correlation, dependencyId));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithCustomTransactionIdPropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryEventHubProducerClient();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var messages = expectedOrders.Select(order => EventDataBuilder.CreateForBody(order).Build());

            var transactionIdPropertyName = "My-Transaction-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendAsync(messages, correlation, logger, options => options.TransactionIdPropertyName = transactionIdPropertyName);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders),
                item => AssertEnrichedEventData(item.First, item.Second, correlation, transactionIdPropertyName: transactionIdPropertyName));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithCustomUpstreamServiceIdPropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryEventHubProducerClient();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var messages = expectedOrders.Select(order => EventDataBuilder.CreateForBody(order).Build());

            var upstreamServicePropertyName = "My-UpstreamService-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendAsync(messages, correlation, logger, options => options.UpstreamServicePropertyName = upstreamServicePropertyName);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders),
                item => AssertEnrichedEventData(item.First, item.Second, correlation, operationParentPropertyName: upstreamServicePropertyName));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithCustomTelemetryContext_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryEventHubProducerClient();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var messages = expectedOrders.Select(order => EventDataBuilder.CreateForBody(order).Build());

            string key = Guid.NewGuid().ToString(), value = Guid.NewGuid().ToString();
            var telemetryContext = new Dictionary<string, object> { [key] = value };
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendAsync(messages, correlation, logger, options => options.AddTelemetryContext(telemetryContext));

            // Assert
            string logMessage = AssertDependencyTelemetry(logger);
            Assert.Contains(key, logMessage);
            Assert.Contains(value, logMessage);
            Assert.All(
                spySender.Messages.Zip(expectedOrders),
                item => AssertEnrichedEventData(item.First, item.Second, correlation));
        }

        private static MessageCorrelationInfo GenerateMessageCorrelationInfo()
        {
            return new MessageCorrelationInfo(
                $"operation-{Guid.NewGuid()}",
                $"transaction-{Guid.NewGuid()}",
                $"parent-{Guid.NewGuid()}");
        }

        private static string AssertDependencyTelemetry(InMemoryLogger logger)
        {
            string logMessage = Assert.Single(logger.Messages);
            Assert.Contains("Dependency", logMessage);
            Assert.Matches(DependencyIdPattern, logMessage);

            return logMessage;
        }

        private static void AssertDependencyTelemetry(InMemoryLogger logger, string dependencyId)
        {
            string logMessage = Assert.Single(logger.Messages);
            Assert.Contains("Dependency", logMessage);
            Assert.Contains($"with ID {dependencyId}", logMessage);
        }

        private static void AssertEnrichedEventData(
            EventData message,
            Order expectedOrder,
            MessageCorrelationInfo correlation,
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentPropertyName = PropertyNames.OperationParentId)
        {
            var actualOrder = JsonConvert.DeserializeObject<Order>(Encoding.UTF8.GetString(message.Body.ToArray()));
            Assert.Equal(expectedOrder.Id, actualOrder.Id);

            Assert.Equal(correlation.TransactionId, Assert.Contains(transactionIdPropertyName, message.Properties));
            var actualOperationParentId = Assert.Contains(operationParentPropertyName, message.Properties).ToString();
            Assert.False(string.IsNullOrWhiteSpace(actualOperationParentId));
        }

        private static void AssertEnrichedEventDataWithDependencyId(
            EventData message,
            Order expectedOrder,
            MessageCorrelationInfo correlation,
            string dependencyId,
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentIdPropertyName = PropertyNames.OperationParentId)
        {
            var actualOrder = message.EventBody.ToObjectFromJson<Order>();
            Assert.Equal(expectedOrder.Id, actualOrder.Id);

            Assert.Equal(correlation.TransactionId, Assert.Contains(transactionIdPropertyName, message.Properties));
            Assert.Equal(dependencyId, Assert.Contains(operationParentIdPropertyName, message.Properties));
        }

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(eventBatch: (IEnumerable<object>) null, correlation, logger));
        }

        [Fact]
        public async Task SendMessageBodiesWithoutOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var order = OrderGenerator.Generate();
            var logger = new InMemoryLogger();

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(new[] { order }, correlationInfo: null, logger));
        }

        [Fact]
        public async Task SendMessageBodiesWithoutOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var order = OrderGenerator.Generate();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(new[] { order }, correlation, logger: null));
        }

        [Fact]
        public async Task SendMessageBodiesWithOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(eventBatch: (IEnumerable<object>) null, correlation, logger, options => { }));
        }

        [Fact]
        public async Task SendMessageBodiesWithOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var order = OrderGenerator.Generate();
            var logger = new InMemoryLogger();

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(new[] { order }, correlationInfo: null, logger, options => { }));
        }

        [Fact]
        public async Task SendMessageBodiesWithOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var order = OrderGenerator.Generate();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(new[] { order }, correlation, logger: null, options => { }));
        }

        [Fact]
        public async Task SendMessageWithoutOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(eventBatch: null, correlation, logger));
        }

        [Fact]
        public async Task SendMessageWithOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(eventBatch: null, correlation, logger, options => { }));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(eventBatch: null, correlation, logger));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var logger = new InMemoryLogger();

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(new[] { order }, correlationInfo: null, logger));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(new[] { order }, correlation, logger: null));
        }

        [Fact]
        public async Task SendMessagesWithOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(eventBatch: null, correlation, logger, options => { }));
        }

        [Fact]
        public async Task SendMessagesWithOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var logger = new InMemoryLogger();

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(new[] { order }, correlationInfo: null, logger, options => { }));
        }

        [Fact]
        public async Task SendMessagesWithOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<EventHubProducerClient>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendAsync(new[] { order }, correlation, logger: null, options => { }));
        }
    }
}
