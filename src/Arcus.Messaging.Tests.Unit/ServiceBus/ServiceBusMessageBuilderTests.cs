using System;
using System.Text;
using System.Text.Json;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Unit.Fixture;
using Azure.Messaging.ServiceBus;
using Bogus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    [Trait("Category", "Unit")]
    public class ServiceBusMessageBuilderTests
    {
        [Fact]
        public void Create_WithDefaults_Succeeds()
        {
            // Arrange
            var messageBody = 1;
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody);

            // Act
            ServiceBusMessage message = builder.Build();

            // Assert
            Assert.NotNull(message);
            Assert.NotNull(message.Body);
            Assert.Equal(messageBody.ToString(), message.Body.ToString());
        }

        [Fact]
        public void CreateWithEncoding_WithDefaults_Succeeds()
        {
            // Arrange
            var messageBody = 1;
            var encoding = Encoding.UTF32;
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody, encoding);

            // Act
            ServiceBusMessage message = builder.Build();

            // Assert
            Assert.NotNull(message);
            Assert.NotNull(message.Body);
            Assert.Equal(messageBody.ToString(), encoding.GetString(message.Body));
        }

        [Fact]
        public void Create_WithoutMessageBody_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => ServiceBusMessageBuilder.CreateForBody(messageBody: null));
        }

        [Fact]
        public void CreateWithEncoding_WithoutMessageBody_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                ServiceBusMessageBuilder.CreateForBody(messageBody: null, Encoding.UTF8));
        }

        [Fact]
        public void CreateWithEncoding_WithoutEncoding_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                ServiceBusMessageBuilder.CreateForBody("message body", encoding: null));
        }

        [Fact]
        public void Create_WithTransactionId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var transactionId = $"transaction-{Guid.NewGuid()}";

            // Act
            builder.WithTransactionId(transactionId);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(transactionId, Assert.Contains(PropertyNames.TransactionId, message.ApplicationProperties));
        }

        [Fact]
        public void Create_WithCustomTransactionIdProperty_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var transactionId = $"transaction-{Guid.NewGuid()}";
            var transactionIdPropertyName = "MyTransactionIdProperty";

            // Act
            builder.WithTransactionId(transactionId, transactionIdPropertyName);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(transactionId, Assert.Contains(transactionIdPropertyName, message.ApplicationProperties));
        }

        [Fact]
        public void Create_WithTransactionIdPropertyNameTwice_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var transactionId = $"transaction-{Guid.NewGuid()}";
            var wrongPropertyName = "MyTransaction";

            // Act
            builder.WithTransactionId(transactionId, wrongPropertyName).WithTransactionId(transactionId);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(transactionId, Assert.Contains(PropertyNames.TransactionId, message.ApplicationProperties));
            Assert.DoesNotContain(wrongPropertyName, message.ApplicationProperties);
        }

        [Fact]
        public void CreateWithDefaultTransactionId_WithoutTransactionId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);

            // Act
            builder.WithTransactionId(transactionId: null);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.DoesNotContain(PropertyNames.TransactionId, message.ApplicationProperties);
        }

        [Fact]
        public void CreateWithCustomTransactionId_WithoutTransactionId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var propertyName = "MyTransactionIdProperty";

            // Act
            builder.WithTransactionId(transactionId: null, propertyName);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.DoesNotContain(propertyName, message.ApplicationProperties);
        }

        [Fact]
        public void CreateWithCustomTransactionId_WithoutTransactionIdPropertyName_Succeeds()
        {
            // Arrange
            Order order = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(order);
            var transactionId = $"transaction-{Guid.NewGuid()}";

            // Act
            builder.WithTransactionId(transactionId, transactionIdPropertyName: null);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(order, message);
            Assert.Equal(transactionId, Assert.Contains(PropertyNames.TransactionId, message.ApplicationProperties));
        }

        [Fact]
        public void Create_WithOperationId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var operationId = $"operation-{Guid.NewGuid()}";

            // Act
            builder.WithOperationId(operationId);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationId, message.CorrelationId);
        }

        [Fact]
        public void Create_WithOperationIdPropertyName_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var operationId = $"operation-{Guid.NewGuid()}";
            var operationIdPropertyName = "MyOperation";

            // Act
            builder.WithOperationId(operationId, operationIdPropertyName);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.NotEqual(operationId, message.CorrelationId);
            Assert.Equal(operationId, Assert.Contains(operationIdPropertyName, message.ApplicationProperties));
        }

        [Fact]
        public void Create_WithoutOperationId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);

            // Act
            builder.WithOperationId(operationId: null);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Null(message.CorrelationId);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateWithPropertyName_WithoutOperationId_Succeeds(string operationId)
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);

            // Act
            builder.WithOperationId(operationId, "MyOperationIdProperty");

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Null(message.CorrelationId);
        }

        [Fact]
        public void CreateWithPropertyName_WithoutOperationIdPropertyName_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var operationId = $"operation-{Guid.NewGuid()}";

            // Act
            builder.WithOperationId(operationId, operationIdPropertyName: null);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationId, message.CorrelationId);
        }

        [Fact]
        public void Create_WithOperationIdPropertyNameTwice_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var operationId = $"operation-{Guid.NewGuid()}";
            var wrongPropertyName = "MyOperation";

            // Act
            builder.WithOperationId(operationId, wrongPropertyName).WithOperationId(operationId);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationId, message.CorrelationId);
            Assert.DoesNotContain(wrongPropertyName, message.ApplicationProperties);
        }

        [Fact]
        public void Create_WithOperationParentId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";

            // Act
            builder.WithOperationParentId(operationParentId);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationParentId, Assert.Contains(PropertyNames.OperationParentId, message.ApplicationProperties));
        }

        [Fact]
        public void Create_WithCustomOperationParentIdProperty_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";
            var operationParentIdPropertyName = "MyOperationParentIdProperty";

            // Act
            builder.WithOperationParentId(operationParentId, operationParentIdPropertyName);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationParentId, Assert.Contains(operationParentIdPropertyName, message.ApplicationProperties));
        }

        [Fact]
        public void Create_WithOperationParentIdPropertyNameTwice_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var operationParentId = $"operation-{Guid.NewGuid()}";
            var wrongPropertyName = "MyOperation";

            // Act
            builder.WithOperationParentId(operationParentId, wrongPropertyName).WithOperationParentId(operationParentId);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationParentId, Assert.Contains(PropertyNames.OperationParentId, message.ApplicationProperties));
            Assert.DoesNotContain(wrongPropertyName, message.ApplicationProperties);
        }

        [Fact]
        public void CreateWithDefaultOperationParentId_WithoutOperationParentId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);

            // Act
            builder.WithOperationParentId(operationParentId: null);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.DoesNotContain(PropertyNames.OperationParentId, message.ApplicationProperties);
        }

        [Fact]
        public void CreateWithCustomOperationParentId_WithoutOperationParentId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var propertyName = "MyOperationParentIdProperty";

            // Act
            builder.WithOperationParentId(operationParentId: null, propertyName);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.DoesNotContain(propertyName, message.ApplicationProperties);
        }

        [Fact]
        public void CreateWithCustomOperationParentId_WithoutOperationParentIdPropertyName_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = ServiceBusMessageBuilder.CreateForBody(expected);
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";

            // Act
            builder.WithOperationParentId(operationParentId, operationParentIdPropertyName: null);

            // Assert
            ServiceBusMessage message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationParentId, Assert.Contains(PropertyNames.OperationParentId, message.ApplicationProperties));
        }

        private static Order GenerateOrder()
        {
            Faker<Order> generator = new Faker<Order>()
                .RuleFor(order => order.OrderId, faker => faker.Random.Hash())
                .RuleFor(order => order.CustomerName, faker => faker.Name.FullName());

            Order order = generator.Generate();
            return order;
        }

        private static void AssertEqualOrder(Order expected, ServiceBusMessage message)
        {
            var actual = JsonSerializer.Deserialize<Order>(message.Body);
            Assert.Equal(expected, actual);
        }
    }
}
