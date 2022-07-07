using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Unit.ServiceBus.Fixture;
using Arcus.Testing.Logging;
using Azure.Messaging.ServiceBus;
using Bogus;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    public class ServiceBusSenderExtensionsTests
    {
        private const string DependencyIdPattern = @"with ID [a-z0-9]{8}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{4}\-[a-z0-9]{12}";

        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public async Task SendMessageBodyWithoutOptions_WithMessageCorrelation_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrder = OrderGenerator.Generate();
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessageAsync(expectedOrder, correlation, logger);

            // Assert
            AssertDependencyTelemetry(logger);

            ServiceBusMessage message = Assert.Single(spySender.Messages);
            AssertEnrichedServiceBusMessage(message, expectedOrder, correlation);
        }

        [Fact]
        public async Task SendMessageBodyWithoutOptions_WithCustomDependencyId_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrder = OrderGenerator.Generate();
            var dependencyId = $"dependency-{Guid.NewGuid()}";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessageAsync(expectedOrder, correlation, logger, options => options.GenerateDependencyId = () => dependencyId);

            // Assert
            AssertDependencyTelemetry(logger, dependencyId);

            ServiceBusMessage message = Assert.Single(spySender.Messages);
            AssertEnrichedServiceBusMessageWithDependencyId(message, expectedOrder, correlation, dependencyId);
        }

        [Fact]
        public async Task SendMessageBodyWithoutOptions_WithCustomTransactionIdPropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrder = OrderGenerator.Generate();
            var transactionIdPropertyName = "My-Transaction-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessageAsync(expectedOrder, correlation, logger, options => options.TransactionIdPropertyName = transactionIdPropertyName);

            // Assert
            AssertDependencyTelemetry(logger);

            ServiceBusMessage message = Assert.Single(spySender.Messages);
            AssertEnrichedServiceBusMessage(message, expectedOrder, correlation, transactionIdPropertyName: transactionIdPropertyName);
        }

        [Fact]
        public async Task SendMessageBodyWithoutOptions_WithCustomUpstreamServicePropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrder = OrderGenerator.Generate();
            var upstreamServicePropertyName = "My-UpstreamService-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessageAsync(expectedOrder, correlation, logger, options => options.UpstreamServicePropertyName = upstreamServicePropertyName);

            // Assert
            AssertDependencyTelemetry(logger);

            ServiceBusMessage message = Assert.Single(spySender.Messages);
            AssertEnrichedServiceBusMessage(message, expectedOrder, correlation, operationParentPropertyName: upstreamServicePropertyName);
        }

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithMessageCorrelation_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessagesAsync(expectedOrders, correlation, logger);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders), 
                item => AssertEnrichedServiceBusMessage(item.First, item.Second, correlation));
        }

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithCustomDependencyId_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var dependencyId = $"dependency-{Guid.NewGuid()}";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessagesAsync(expectedOrders, correlation, logger, options => options.GenerateDependencyId = () => dependencyId);

            // Assert
            AssertDependencyTelemetry(logger, dependencyId);
            Assert.All(
                spySender.Messages.Zip(expectedOrders), 
                item => AssertEnrichedServiceBusMessageWithDependencyId(item.First, item.Second, correlation, dependencyId));
        }

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithCustomTransactionIdPropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var transactionIdPropertyName = "My-Transaction-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessagesAsync(expectedOrders, correlation, logger, options => options.TransactionIdPropertyName = transactionIdPropertyName);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders), 
                item => AssertEnrichedServiceBusMessage(item.First, item.Second, correlation, transactionIdPropertyName: transactionIdPropertyName));
        }

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithCustomUpstreamServiceIdPropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var upstreamServicePropertyName = "My-UpstreamService-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessagesAsync(expectedOrders, correlation, logger, options => options.UpstreamServicePropertyName = upstreamServicePropertyName);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders), 
                item => AssertEnrichedServiceBusMessage(item.First, item.Second, correlation, operationParentPropertyName: upstreamServicePropertyName));
        }

         [Fact]
        public async Task SendMessageWithoutOptions_WithMessageCorrelation_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrder = OrderGenerator.Generate();
            var expected = ServiceBusMessageBuilder.CreateForBody(expectedOrder).Build();

            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessageAsync(expected, correlation, logger);

            // Assert
            AssertDependencyTelemetry(logger);

            ServiceBusMessage message = Assert.Single(spySender.Messages);
            AssertEnrichedServiceBusMessage(message, expectedOrder, correlation);
        }

        [Fact]
        public async Task SendMessageWithoutOptions_WithCustomDependencyId_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrder = OrderGenerator.Generate();
            var expected = ServiceBusMessageBuilder.CreateForBody(expectedOrder).Build();

            var dependencyId = $"dependency-{Guid.NewGuid()}";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessageAsync(expected, correlation, logger, options => options.GenerateDependencyId = () => dependencyId);

            // Assert
            AssertDependencyTelemetry(logger, dependencyId);

            ServiceBusMessage message = Assert.Single(spySender.Messages);
            AssertEnrichedServiceBusMessageWithDependencyId(message, expectedOrder, correlation, dependencyId);
        }

        [Fact]
        public async Task SendMessageWithoutOptions_WithCustomTransactionIdPropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrder = OrderGenerator.Generate();
            var expected = ServiceBusMessageBuilder.CreateForBody(expectedOrder).Build();

            var transactionIdPropertyName = "My-Transaction-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessageAsync(expected, correlation, logger, options => options.TransactionIdPropertyName = transactionIdPropertyName);

            // Assert
            AssertDependencyTelemetry(logger);

            ServiceBusMessage message = Assert.Single(spySender.Messages);
            AssertEnrichedServiceBusMessage(message, expectedOrder, correlation, transactionIdPropertyName: transactionIdPropertyName);
        }

        [Fact]
        public async Task SendMessageWithoutOptions_WithCustomUpstreamServicePropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrder = OrderGenerator.Generate();
            var expected = ServiceBusMessageBuilder.CreateForBody(expectedOrder).Build();
            
            var upstreamServicePropertyName = "My-UpstreamService-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessageAsync(expected, correlation, logger, options => options.UpstreamServicePropertyName = upstreamServicePropertyName);

            // Assert
            AssertDependencyTelemetry(logger);

            ServiceBusMessage message = Assert.Single(spySender.Messages);
            AssertEnrichedServiceBusMessage(message, expectedOrder, correlation, operationParentPropertyName: upstreamServicePropertyName);
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithMessageCorrelation_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var messages = expectedOrders.Select(order => ServiceBusMessageBuilder.CreateForBody(order).Build());

            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessagesAsync(messages, correlation, logger);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders), 
                item => AssertEnrichedServiceBusMessage(item.First, item.Second, correlation));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithCustomDependencyId_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var messages = expectedOrders.Select(order => ServiceBusMessageBuilder.CreateForBody(order).Build());

            var dependencyId = $"dependency-{Guid.NewGuid()}";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessagesAsync(messages, correlation, logger, options => options.GenerateDependencyId = () => dependencyId);

            // Assert
            AssertDependencyTelemetry(logger, dependencyId);
            Assert.All(
                spySender.Messages.Zip(expectedOrders), 
                item => AssertEnrichedServiceBusMessageWithDependencyId(item.First, item.Second, correlation, dependencyId));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithCustomTransactionIdPropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var messages = expectedOrders.Select(order => ServiceBusMessageBuilder.CreateForBody(order).Build());

            var transactionIdPropertyName = "My-Transaction-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessagesAsync(messages, correlation, logger, options => options.TransactionIdPropertyName = transactionIdPropertyName);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders), 
                item => AssertEnrichedServiceBusMessage(item.First, item.Second, correlation, transactionIdPropertyName: transactionIdPropertyName));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithCustomUpstreamServiceIdPropertyName_Succeeds()
        {
            // Arrange
            var spySender = new InMemoryServiceBusSender();
            var expectedOrders = BogusGenerator.Make(5, () => OrderGenerator.Generate());
            var messages = expectedOrders.Select(order => ServiceBusMessageBuilder.CreateForBody(order).Build());

            var upstreamServicePropertyName = "My-UpstreamService-Id";
            MessageCorrelationInfo correlation = GenerateMessageCorrelationInfo();
            var logger = new InMemoryLogger();

            // Act
            await spySender.SendMessagesAsync(messages, correlation, logger, options => options.UpstreamServicePropertyName = upstreamServicePropertyName);

            // Assert
            AssertDependencyTelemetry(logger);
            Assert.All(
                spySender.Messages.Zip(expectedOrders), 
                item => AssertEnrichedServiceBusMessage(item.First, item.Second, correlation, operationParentPropertyName: upstreamServicePropertyName));
        }

        private static MessageCorrelationInfo GenerateMessageCorrelationInfo()
        {
            return new MessageCorrelationInfo(
                $"operation-{Guid.NewGuid()}",
                $"transaction-{Guid.NewGuid()}",
                $"parent-{Guid.NewGuid()}");
        }

        private static void AssertDependencyTelemetry(InMemoryLogger logger)
        {
            string logMessage = Assert.Single(logger.Messages);
            Assert.Contains("Dependency", logMessage);
            Assert.Matches(DependencyIdPattern, logMessage);
        }

        private static void AssertDependencyTelemetry(InMemoryLogger logger, string dependencyId)
        {
            string logMessage = Assert.Single(logger.Messages);
            Assert.Contains("Dependency", logMessage);
            Assert.Contains($"with ID {dependencyId}", logMessage);
        }

        private static void AssertEnrichedServiceBusMessage(
            ServiceBusMessage message,
            Order expectedOrder,
            MessageCorrelationInfo correlation,
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentPropertyName = PropertyNames.OperationParentId)
        {
            var actualOrder = message.Body.ToObjectFromJson<Order>();
            Assert.Equal(expectedOrder.Id, actualOrder.Id);

            Assert.Equal(correlation.TransactionId, Assert.Contains(transactionIdPropertyName, message.ApplicationProperties));
            var actualOperationParentId = Assert.Contains(operationParentPropertyName, message.ApplicationProperties).ToString();
            Assert.False(string.IsNullOrWhiteSpace(actualOperationParentId));
        }

        private static void AssertEnrichedServiceBusMessageWithDependencyId(
            ServiceBusMessage message,
            Order expectedOrder,
            MessageCorrelationInfo correlation,
            string dependencyId,
            string transactionIdPropertyName = PropertyNames.TransactionId,
            string operationParentIdPropertyName = PropertyNames.OperationParentId)
        {
            var actualOrder = message.Body.ToObjectFromJson<Order>();
            Assert.Equal(expectedOrder.Id, actualOrder.Id);

            Assert.Equal(correlation.TransactionId, Assert.Contains(transactionIdPropertyName, message.ApplicationProperties));
            Assert.Equal(dependencyId, Assert.Contains(operationParentIdPropertyName, message.ApplicationProperties));
        }

        [Fact]
        public async Task SendMessageBodyWithoutOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(messageBody: null, correlation, logger));
        }

        [Fact]
        public async Task SendMessageBodyWithoutOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = OrderGenerator.Generate();
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(order, correlationInfo: null, logger));
        }

        [Fact]
        public async Task SendMessageBodyWithoutOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = OrderGenerator.Generate();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(order, correlation, logger: null));
        }

        [Fact]
        public async Task SendMessageBodyWithOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(messageBody: null, correlation, logger, options => { }));
        }

        [Fact]
        public async Task SendMessageBodyWithOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = OrderGenerator.Generate();
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(order, correlationInfo: null, logger, options => { }));
        }

        [Fact]
        public async Task SendMessageBodyWithOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = OrderGenerator.Generate();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(order, correlation, logger: null, options => { }));
        }

        [Fact]
        public async Task SendMessagesBodyWithoutOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(messageBodies: null, correlation, logger));
        }

        [Fact]
        public async Task SendMessageBodiesWithoutOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = OrderGenerator.Generate();
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(new[] { order }, correlationInfo: null, logger));
        }

        [Fact]
        public async Task SendMessageBodiesWithoutOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = OrderGenerator.Generate();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(new[] { order }, correlation, logger: null));
        }

        [Fact]
        public async Task SendMessageBodiesWithOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(messageBodies: null, correlation, logger, options => { }));
        }

        [Fact]
        public async Task SendMessageBodiesWithOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = OrderGenerator.Generate();
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(new[] { order }, correlationInfo: null, logger, options => { }));
        }

        [Fact]
        public async Task SendMessageBodiesWithOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = OrderGenerator.Generate();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(new[] { order }, correlation, logger: null, options => { }));
        }

        [Fact]
        public async Task SendMessageWithoutOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(message: null, correlation, logger));
        }

        [Fact]
        public async Task SendMessageWithoutOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(order, correlationInfo: null, logger));
        }

        [Fact]
        public async Task SendMessageWithoutOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(order, correlation, logger: null));
        }

        [Fact]
        public async Task SendMessageWithOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(message: null, correlation, logger, options => { }));
        }

        [Fact]
        public async Task SendMessageWithOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(order, correlationInfo: null, logger, options => { }));
        }

        [Fact]
        public async Task SendMessageWithOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessageAsync(order, correlation, logger: null, options => { }));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(messages: null, correlation, logger));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(new[] { order }, correlationInfo: null, logger));
        }

        [Fact]
        public async Task SendMessagesWithoutOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(new[] { order }, correlation, logger: null));
        }

        [Fact]
        public async Task SendMessagesWithOptions_WithoutMessageBody_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(messages: null, correlation, logger, options => { }));
        }

        [Fact]
        public async Task SendMessagesWithOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var logger = new InMemoryLogger();
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(new[] { order }, correlationInfo: null, logger, options => { }));
        }

        [Fact]
        public async Task SendMessagesWithOptions_WithoutLogger_Fails()
        {
            // Arrange
            var sender = Mock.Of<ServiceBusSender>();
            var order = ServiceBusMessageBuilder.CreateForBody(OrderGenerator.Generate()).Build();
            var correlation = new MessageCorrelationInfo("operation ID", "transaction ID", "parent ID");
            
            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => sender.SendMessagesAsync(new[] { order }, correlation, logger: null, options => { }));
        }
    }
}
