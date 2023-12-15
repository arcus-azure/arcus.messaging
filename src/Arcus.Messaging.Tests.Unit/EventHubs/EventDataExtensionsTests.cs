using System;
using System.Linq;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Observability.Telemetry.Core;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using Bogus;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.EventHubs
{
    public class EventDataExtensionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void GetCorrelationInfo_WithoutProperties_Succeeds()
        {
            // Arrange
            var data = new EventData();

            // Act
            MessageCorrelationInfo correlationInfo = data.GetCorrelationInfo();

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.OperationId));
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.TransactionId));
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.CycleId));
            Assert.Null(correlationInfo.OperationParentId);
        }

        [Fact]
        public void GetCorrelationInfo_WithDefaultTransactionIdProperty_Succeeds()
        {
            // Arrange
            var data = new EventData();
            var transactionId = Guid.NewGuid().ToString();
            data.Properties[PropertyNames.TransactionId] = transactionId;

            // Act
            MessageCorrelationInfo correlationInfo = data.GetCorrelationInfo();

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.OperationId));
            Assert.Equal(transactionId, correlationInfo.TransactionId);
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.CycleId));
            Assert.Null(correlationInfo.OperationParentId);
        }

        [Fact]
        public void GetCorrelationInfo_WithDefaultOperationParentIdProperty_Succeeds()
        {
            // Arrange
            var data = new EventData();
            var operationParentId = Guid.NewGuid().ToString();
            data.Properties[PropertyNames.OperationParentId] = operationParentId;

            // Act
            MessageCorrelationInfo correlationInfo = data.GetCorrelationInfo();

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.OperationId));
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.TransactionId));
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.CycleId));
            Assert.Equal(operationParentId, correlationInfo.OperationParentId);
        }

        [Fact]
        public void GetCorrelationInfo_WithCorrelationIdProperty_Succeeds()
        {
            // Arrange
            var data = new EventData();
            var operationId = Guid.NewGuid().ToString();
            data.CorrelationId = operationId;

            // Act
            MessageCorrelationInfo correlationInfo = data.GetCorrelationInfo();

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.Equal(operationId, correlationInfo.OperationId);
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.TransactionId));
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.CycleId));
            Assert.Null(correlationInfo.OperationParentId);
        }

        [Fact]
        public void GetCorrelationInfo_WithCustomTransactionIdProperty_Succeeds()
        {
            // Arrange
            var data = new EventData();
            var propertyName = $"MyTransactionId-{Guid.NewGuid()}";
            var transactionId = Guid.NewGuid().ToString();
            data.Properties[propertyName] = transactionId;
            
            // Act
            MessageCorrelationInfo correlationInfo = data.GetCorrelationInfo(propertyName);

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.OperationId));
            Assert.Equal(transactionId, correlationInfo.TransactionId);
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.CycleId));
            Assert.Null(correlationInfo.OperationParentId);
        }

        [Fact]
        public void GetCorrelationInfo_WithCustomOperationParentIdProperty_Succeeds()
        {
            // Arrange
            var data = new EventData();
            var transactionIdPropertyName = $"MyTransactionId-{Guid.NewGuid()}";
            var transactionId = Guid.NewGuid().ToString();
            data.Properties[transactionIdPropertyName] = transactionId;
            var operationParentIdPropertyName = $"MyOperationParentId-{Guid.NewGuid()}";
            var operationParentId = Guid.NewGuid().ToString();
            data.Properties[operationParentIdPropertyName] = operationParentId;

            // Act
            MessageCorrelationInfo correlationInfo = data.GetCorrelationInfo(transactionIdPropertyName, operationParentIdPropertyName);

            // Assert
            Assert.NotNull(correlationInfo);
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.OperationId));
            Assert.Equal(transactionId, correlationInfo.TransactionId);
            Assert.False(string.IsNullOrWhiteSpace(correlationInfo.CycleId));
            Assert.Equal(operationParentId, correlationInfo.OperationParentId);
        }

        [Fact]
        public void GetMessageContextManually_WithValidArguments_Succeeds()
        {
            // Arrange
            var data = new EventData();
            data.ContentType = BogusGenerator.Random.AlphaNumeric(10);
            string eventHubsNamespace = BogusGenerator.Random.AlphaNumeric(20);
            string eventHubsName = BogusGenerator.Random.AlphaNumeric(5);
            string consumerGroup = BogusGenerator.Random.AlphaNumeric(10);

            // Act
            AzureEventHubsMessageContext context = data.GetMessageContext(eventHubsNamespace, eventHubsName, consumerGroup);

            // Assert
            Assert.Equal(eventHubsNamespace, context.EventHubsNamespace);
            Assert.Equal(eventHubsName, context.EventHubsName);
            Assert.Equal(consumerGroup, context.ConsumerGroup);
            Assert.Equal(data.ContentType, context.ContentType);
            Assert.Equal(data.EnqueuedTime, context.EnqueueTime);
            Assert.Equal(data.Offset, context.Offset);
            Assert.Equal(data.PartitionKey, context.PartitionKey);
            Assert.Equal(data.SequenceNumber, context.SequenceNumber);
            Assert.True(data.SystemProperties.SequenceEqual(context.SystemProperties));
        }

        [Fact]
        public void GetMessageContextViaProcessor_WithValidArguments_Succeeds()
        {
            // Arrange
            var data = new EventData();
            data.ContentType = BogusGenerator.Random.AlphaNumeric(10);
            string eventHubsNamespace = BogusGenerator.Random.AlphaNumeric(20);
            string eventHubsName = BogusGenerator.Random.AlphaNumeric(5);
            string consumerGroup = BogusGenerator.Random.AlphaNumeric(10);

            var processor = new EventProcessorClient(
                Mock.Of<BlobContainerClient>(),
                consumerGroup,
                eventHubsNamespace,
                eventHubsName,
                new DefaultAzureCredential());

            // Act
            AzureEventHubsMessageContext context = data.GetMessageContext(processor);

            // Assert
            Assert.Equal(eventHubsNamespace, context.EventHubsNamespace);
            Assert.Equal(eventHubsName, context.EventHubsName);
            Assert.Equal(consumerGroup, context.ConsumerGroup);
            Assert.Equal(data.ContentType, context.ContentType);
            Assert.Equal(data.EnqueuedTime, context.EnqueueTime);
            Assert.Equal(data.Offset, context.Offset);
            Assert.Equal(data.PartitionKey, context.PartitionKey);
            Assert.Equal(data.SequenceNumber, context.SequenceNumber);
            Assert.True(data.SystemProperties.SequenceEqual(context.SystemProperties));
        }
    }
}
