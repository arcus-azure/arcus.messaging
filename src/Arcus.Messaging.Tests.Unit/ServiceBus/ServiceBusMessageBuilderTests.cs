﻿using System;
using System.Text;
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus;
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
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);
            var transactionId = $"transaction-{Guid.NewGuid()}";

            // Act
            builder.WithTransactionId(transactionId);

            // Assert
            ServiceBusMessage message = builder.Build();
            Assert.Equal(transactionId, message.ApplicationProperties[PropertyNames.TransactionId]);
        }

        [Fact]
        public void Create_WithCustomTransactionIdProperty_Succeeds()
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);
            var transactionId = $"transaction-{Guid.NewGuid()}";
            var transactionIdPropertyName = "MyTransactionIdProperty";

            // Act
            builder.WithTransactionId(transactionId, transactionIdPropertyName);

            // Assert
            ServiceBusMessage message = builder.Build();
            Assert.Equal(transactionId, message.ApplicationProperties[transactionIdPropertyName]);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateWithDefaultTransactionId_WithoutTransactionId_Fails(string transactionId)
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.WithTransactionId(transactionId));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateWithCustomTransactionId_WithoutTransactionId_Fails(string transactionId)
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                builder.WithTransactionId(transactionId, "MyTransactionIdProperty"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateWithCustomTransactionId_WithoutTransactionIdPropertyName_Fails(string transactionIdPropertyName)
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);
            var transactionId = $"transaction-{Guid.NewGuid()}";

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.WithTransactionId(transactionId, transactionIdPropertyName));
        }

        [Fact]
        public void Create_WithOperationId_Succeeds()
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);
            var operationId = $"operation-{Guid.NewGuid()}";

            // Act
            builder.WithCorrelationId(operationId);

            // Assert
            ServiceBusMessage message = builder.Build();
            Assert.Equal(operationId, message.CorrelationId);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void Create_WithoutOperationId_Fails(string operationId)
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.WithCorrelationId(operationId));
        }

        [Fact]
        public void Create_WithOperationParentId_Succeeds()
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";

            // Act
            builder.WithOperationParentId(operationParentId);

            // Assert
            ServiceBusMessage message = builder.Build();
            Assert.Equal(operationParentId, message.ApplicationProperties[PropertyNames.OperationParentId]);
        }

        [Fact]
        public void Create_WithCustomOperationParentIdProperty_Succeeds()
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";
            var operationParentIdPropertyName = "MyOperationParentIdProperty";

            // Act
            builder.WithOperationParentId(operationParentId, operationParentIdPropertyName);

            // Assert
            ServiceBusMessage message = builder.Build();
            Assert.Equal(operationParentId, message.ApplicationProperties[operationParentIdPropertyName]);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateWithDefaultOperationParentId_WithoutOperationParentId_Fails(string operationParentId)
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.WithOperationParentId(operationParentId));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateWithCustomOperationParentId_WithoutOperationParentId_Fails(string operationParentId)
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                builder.WithOperationParentId(operationParentId, "MyOperationParentIdProperty"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateWithCustomOperationParentId_WithoutOperationParentIdPropertyName_Fails(string operationParentIdPropertyName)
        {
            // Arrange
            var builder = ServiceBusMessageBuilder.CreateForBody(messageBody: 1);
            var operationParentId = $"operation-parent-{Guid.NewGuid()}";

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => builder.WithOperationParentId(operationParentId, operationParentIdPropertyName));
        }
    }
}
