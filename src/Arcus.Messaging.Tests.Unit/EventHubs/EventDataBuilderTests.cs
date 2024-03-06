using System;
using System.Text;
using System.Text.Json;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Tests.Unit.Fixture;
using Azure.Messaging.EventHubs;
using Bogus;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.EventHubs
{
    public class EventDataBuilderTests
    {
        [Fact]
        public void Create_WithDefaults_Succeeds()
        {
            // Arrange
            var messageBody = 1;
            var builder = EventDataBuilder.CreateForBody(messageBody);

            // Act
            EventData message = builder.Build();

            // Assert
            Assert.NotNull(message);
            Assert.NotEmpty(message.Body.ToArray());
            Assert.Equal(messageBody.ToString(), Encoding.UTF8.GetString(message.Body.ToArray()));
            Encoding encoding = message.GetMessageContext("namespace", "consumergroup", "name").GetMessageEncodingProperty();
            Assert.Equal(Encoding.UTF8, encoding);
        }

        [Fact]
        public void CreateWithEncoding_WithDefaults_Succeeds()
        {
            // Arrange
            var messageBody = 1;
            var expectedEncoding = Encoding.UTF32;
            var builder = EventDataBuilder.CreateForBody(messageBody, expectedEncoding);

            // Act
            EventData message = builder.Build();

            // Assert
            Assert.NotNull(message);
            Assert.NotEmpty(message.Body.ToArray());
            Assert.Equal(messageBody.ToString(), expectedEncoding.GetString(message.Body.ToArray()));
            Encoding actualEncoding = message.GetMessageContext("namespace", "consumergroup", "name").GetMessageEncodingProperty();
            Assert.Equal(expectedEncoding, actualEncoding);
        }

        [Fact]
        public void Create_WithoutMessageBody_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => EventDataBuilder.CreateForBody(eventBody: null));
        }

        [Fact]
        public void CreateWithEncoding_WithoutMessageBody_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                EventDataBuilder.CreateForBody(eventBody: null, Encoding.UTF8));
        }

        [Fact]
        public void CreateWithEncoding_WithoutEncoding_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                EventDataBuilder.CreateForBody("message body", encoding: null));
        }

        [Fact]
        public void Create_WithTransactionId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var transactionId = $"transaction-{Guid.NewGuid()}";

            // Act
            builder.WithTransactionId(transactionId);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(transactionId, Assert.Contains(PropertyNames.TransactionId, message.Properties));
        }

        [Fact]
        public void Create_WithCustomTransactionIdProperty_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var transactionId = $"transaction-{Guid.NewGuid()}";
            var transactionIdPropertyName = "MyTransactionIdProperty";

            // Act
            builder.WithTransactionId(transactionId, transactionIdPropertyName);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(transactionId, Assert.Contains(transactionIdPropertyName, message.Properties));
        }

        [Fact]
        public void Create_WithTransactionIdPropertyNameTwice_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var transactionId = $"transaction-{Guid.NewGuid()}";
            var wrongPropertyName = "MyTransaction";

            // Act
            builder.WithTransactionId(transactionId, wrongPropertyName).WithTransactionId(transactionId);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(transactionId, Assert.Contains(PropertyNames.TransactionId, message.Properties));
            Assert.DoesNotContain(wrongPropertyName, message.Properties);
        }

        [Fact]
        public void CreateWithDefaultTransactionId_WithoutTransactionId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);

            // Act
            builder.WithTransactionId(transactionId: null);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.DoesNotContain(PropertyNames.TransactionId, message.Properties);
        }

        [Fact]
        public void CreateWithCustomTransactionId_WithoutTransactionId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var propertyName = "MyTransactionIdProperty";

            // Act
            builder.WithTransactionId(transactionId: null, propertyName);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.DoesNotContain(propertyName, message.Properties);
        }

        [Fact]
        public void CreateWithCustomTransactionId_WithoutTransactionIdPropertyName_Succeeds()
        {
            // Arrange
            Order order = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(order);
            var transactionId = $"transaction-{Guid.NewGuid()}";

            // Act
            builder.WithTransactionId(transactionId, transactionIdPropertyName: null);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(order, message);
            Assert.Equal(transactionId, Assert.Contains(PropertyNames.TransactionId, message.Properties));
        }

        [Fact]
        public void Create_WithOperationId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var operationId = $"operation-{Guid.NewGuid()}";

            // Act
            builder.WithOperationId(operationId);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationId, message.CorrelationId);
        }

        [Fact]
        public void Create_WithOperationIdPropertyName_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var operationId = $"operation-{Guid.NewGuid()}";
            var operationIdPropertyName = "MyOperation";

            // Act
            builder.WithOperationId(operationId, operationIdPropertyName);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.NotEqual(operationId, message.CorrelationId);
            Assert.Equal(operationId, Assert.Contains(operationIdPropertyName, message.Properties));
        }

        [Fact]
        public void Create_WithoutOperationId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);

            // Act
            builder.WithOperationId(operationId: null);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Null(message.CorrelationId);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateWithPropertyName_WithoutOperationId_Succeeds(string operationId)
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);

            // Act
            builder.WithOperationId(operationId, "MyOperationIdProperty");

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Null(message.CorrelationId);
        }

        [Fact]
        public void CreateWithPropertyName_WithoutOperationIdPropertyName_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var operationId = $"operation-{Guid.NewGuid()}";

            // Act
            builder.WithOperationId(operationId, operationIdPropertyName: null);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationId, message.CorrelationId);
        }

        [Fact]
        public void Create_WithOperationIdPropertyNameTwice_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var operationId = $"operation-{Guid.NewGuid()}";
            var wrongPropertyName = "MyOperation";

            // Act
            builder.WithOperationId(operationId, wrongPropertyName).WithOperationId(operationId);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationId, message.CorrelationId);
            Assert.DoesNotContain(wrongPropertyName, message.Properties);
        }

        [Fact]
        public void Create_WithOperationParentId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";

            // Act
            builder.WithOperationParentId(operationParentId);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationParentId, Assert.Contains(PropertyNames.OperationParentId, message.Properties));
        }

        [Fact]
        public void Create_WithCustomOperationParentIdProperty_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";
            var operationParentIdPropertyName = "MyOperationParentIdProperty";

            // Act
            builder.WithOperationParentId(operationParentId, operationParentIdPropertyName);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationParentId, Assert.Contains(operationParentIdPropertyName, message.Properties));
        }

        [Fact]
        public void Create_WithOperationParentIdPropertyNameTwice_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var operationParentId = $"operation-{Guid.NewGuid()}";
            var wrongPropertyName = "MyOperation";

            // Act
            builder.WithOperationParentId(operationParentId, wrongPropertyName).WithOperationParentId(operationParentId);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationParentId, Assert.Contains(PropertyNames.OperationParentId, message.Properties));
            Assert.DoesNotContain(wrongPropertyName, message.Properties);
        }

        [Fact]
        public void CreateWithDefaultOperationParentId_WithoutOperationParentId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);

            // Act
            builder.WithOperationParentId(operationParentId: null);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.DoesNotContain(PropertyNames.OperationParentId, message.Properties);
        }

        [Fact]
        public void CreateWithCustomOperationParentId_WithoutOperationParentId_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var propertyName = "MyOperationParentIdProperty";

            // Act
            builder.WithOperationParentId(operationParentId: null, propertyName);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.DoesNotContain(propertyName, message.Properties);
        }

        [Fact]
        public void CreateWithCustomOperationParentId_WithoutOperationParentIdPropertyName_Succeeds()
        {
            // Arrange
            Order expected = GenerateOrder();
            var builder = EventDataBuilder.CreateForBody(expected);
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";

            // Act
            builder.WithOperationParentId(operationParentId, operationParentIdPropertyName: null);

            // Assert
            EventData message = builder.Build();
            AssertEqualOrder(expected, message);
            Assert.Equal(operationParentId, Assert.Contains(PropertyNames.OperationParentId, message.Properties));
        }

        private static Order GenerateOrder()
        {
            Faker<Order> generator = new Faker<Order>()
                .RuleFor(order => order.OrderId, faker => faker.Random.Hash())
                .RuleFor(order => order.CustomerName, faker => faker.Name.FullName());

            Order order = generator.Generate();
            return order;
        }

        private static void AssertEqualOrder(Order expected, EventData message)
        {
            var actual = JsonSerializer.Deserialize<Order>(message.Body.ToArray());
            Assert.Equal(expected, actual);
        }
    }
}
