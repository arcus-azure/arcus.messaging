using System.Linq;
using Arcus.Messaging.Abstractions.EventHubs;
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
        public void GetMessageContextManually_WithValidArguments_Succeeds()
        {
            // Arrange
            var data = new EventData();
            data.ContentType = BogusGenerator.Random.AlphaNumeric(10);
            string eventHubsNamespace = BogusGenerator.Random.AlphaNumeric(20);
            string eventHubsName = BogusGenerator.Random.AlphaNumeric(5);
            string consumerGroup = BogusGenerator.Random.AlphaNumeric(10);
            string jobId = BogusGenerator.Random.Guid().ToString();

            // Act
            AzureEventHubsMessageContext context = data.GetMessageContext(eventHubsNamespace, consumerGroup, eventHubsName, jobId);

            // Assert
            Assert.Equal(eventHubsNamespace, context.EventHubsNamespace);
            Assert.Equal(eventHubsName, context.EventHubsName);
            Assert.Equal(consumerGroup, context.ConsumerGroup);
            Assert.Equal(jobId, context.JobId);
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
