using System;
using System.Reflection;
using System.Text;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Tests.Unit.Fixture;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Bogus;
using Bogus.Extensions;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    [Trait("Category", "Unit")]
    public class AzureServiceBusSystemPropertiesTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void CreateProperties_WithoutServiceBusMessage_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => AzureServiceBusSystemProperties.CreateFrom(message: null));
        }

        [Fact]
        public void CreateProperties_FromMessage_AssignsDeliveryCountCorrectly()
        {
            // Arrange
            uint expected = BogusGenerator.Random.UInt();

            AmqpAnnotatedMessage amqpMessage = CreateAmqpMessage();
            amqpMessage.Header.DeliveryCount = expected;
            ServiceBusReceivedMessage message = CreateServiceBusReceivedMessage(amqpMessage);

            // Act
            var systemProperties = AzureServiceBusSystemProperties.CreateFrom(message);

            // Assert
            Assert.Equal((int)expected, systemProperties.DeliveryCount);
        }

        [Fact]
        public void CreateProperties_FromMessage_AssignsContentTypeCorrectly()
        {
            // Arrange
            string expected = BogusGenerator.Random.String();

            AmqpAnnotatedMessage amqpMessage = CreateAmqpMessage();
            amqpMessage.Properties.ContentType = expected;
            ServiceBusReceivedMessage message = CreateServiceBusReceivedMessage(amqpMessage);

            // Act
            var systemProperties = AzureServiceBusSystemProperties.CreateFrom(message);

            // Assert
            Assert.Equal(expected, systemProperties.ContentType);
        }

        [Fact]
        public void CreateProperties_FromMessage_AssignsDeadLetterSourceCorrectly()
        {
            // Arrange
            string expected = BogusGenerator.Random.String();

            AmqpAnnotatedMessage amqpMessage = CreateAmqpMessage();
            amqpMessage.MessageAnnotations["x-opt-deadletter-source"] = expected;
            ServiceBusReceivedMessage message = CreateServiceBusReceivedMessage(amqpMessage);

            // Act
            var systemProperties = AzureServiceBusSystemProperties.CreateFrom(message);

            // Assert
            Assert.Equal(expected, systemProperties.DeadLetterSource);
        }

        [Fact]
        public void CreateProperties_FromMessage_AssignsEnqueuedSequenceNumberCorrectly()
        {
            // Arrange
            long expected = BogusGenerator.Random.Long();

            AmqpAnnotatedMessage amqpMessage = CreateAmqpMessage();
            amqpMessage.MessageAnnotations["x-opt-enqueue-sequence-number"] = expected;
            ServiceBusReceivedMessage message = CreateServiceBusReceivedMessage(amqpMessage);

            // Act
            var systemProperties = AzureServiceBusSystemProperties.CreateFrom(message);

            // Assert
            Assert.Equal(expected, systemProperties.EnqueuedSequenceNumber);
        }

        [Fact]
        public void CreateProperties_FromMessage_AssignsIsReceivedCorrectly()
        {
            // Arrange
            long expected = BogusGenerator.Random.Long(min: 0);

            AmqpAnnotatedMessage amqpMessage = CreateAmqpMessage();
            amqpMessage.MessageAnnotations["x-opt-enqueue-sequence-number"] = expected;
            ServiceBusReceivedMessage message = CreateServiceBusReceivedMessage(amqpMessage);

            // Act
            var systemProperties = AzureServiceBusSystemProperties.CreateFrom(message);

            // Assert
            Assert.True(systemProperties.IsReceived);
        }

        [Fact]
        public void CreateProperties_FromMessage_AssignsEnqueuedTimeCorrectly()
        {
            // Arrange
            DateTime expected = BogusGenerator.Date.Recent();

            AmqpAnnotatedMessage amqpMessage = CreateAmqpMessage();
            amqpMessage.MessageAnnotations["x-opt-enqueued-time"] = expected;
            ServiceBusReceivedMessage message = CreateServiceBusReceivedMessage(amqpMessage);

            // Act
            var systemProperties = AzureServiceBusSystemProperties.CreateFrom(message);

            // Assert
            Assert.Equal(expected, systemProperties.EnqueuedTime);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("465924F2-D386-407F-826A-573144085682")]
        public void CreateProperties_FromMessage_AssignsLockTokenCorrectly(string lockToken)
        {
            // Arrange
            Guid.TryParse(lockToken, out Guid expected);
            AmqpAnnotatedMessage amqpMessage = CreateAmqpMessage();
            ServiceBusReceivedMessage message = CreateServiceBusReceivedMessage(amqpMessage);
            SetLockToken(message, expected);

            // Act
            var systemProperties = AzureServiceBusSystemProperties.CreateFrom(message);

            // Assert
            Assert.Equal(expected, Guid.Parse(systemProperties.LockToken));
            Assert.Equal(lockToken != null, systemProperties.IsLockTokenSet);
        }

        [Fact]
        public void CreateProperties_FromMessage_AssignsLockedUntilCorrectly()
        {
            // Arrange
            DateTime expected = BogusGenerator.Date.Past();

            AmqpAnnotatedMessage amqpMessage = CreateAmqpMessage();
            amqpMessage.MessageAnnotations["x-opt-locked-until"] = expected;
            ServiceBusReceivedMessage message = CreateServiceBusReceivedMessage(amqpMessage);

            // Act
            var systemProperties = AzureServiceBusSystemProperties.CreateFrom(message);

            // Assert
            Assert.Equal(expected, systemProperties.LockedUntil);
        }

        [Fact]
        public void CreateProperties_FromMessage_AssignsSequenceNumberCorrectly()
        {
            // Arrange
            long expected = BogusGenerator.Random.Long();

            AmqpAnnotatedMessage amqpMessage = CreateAmqpMessage();
            amqpMessage.MessageAnnotations["x-opt-sequence-number"] = expected;
            ServiceBusReceivedMessage message = CreateServiceBusReceivedMessage(amqpMessage);

            // Act
            var systemProperties = AzureServiceBusSystemProperties.CreateFrom(message);

            // Assert
            Assert.Equal(expected, systemProperties.SequenceNumber);
            Assert.Equal(expected > -1, systemProperties.IsReceived);
        }

        private static void SetLockToken(ServiceBusReceivedMessage message, Guid? expected)
        {
            if (expected == null || expected == Guid.Empty)
            {
                return;
            }

            PropertyInfo lockTokenGuid = message.GetType().GetProperty("LockTokenGuid", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(lockTokenGuid);
            lockTokenGuid.SetValue(message, expected);
        }

        private static AmqpAnnotatedMessage CreateAmqpMessage()
        {
            var order = new Order();
            string json = JsonConvert.SerializeObject(order);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            var message = new AmqpAnnotatedMessage(new AmqpMessageBody(new[] { new ReadOnlyMemory<byte>(bytes) }));
            message.Header.DeliveryCount = BogusGenerator.Random.UInt();

            return message;
        }

        private static ServiceBusReceivedMessage CreateServiceBusReceivedMessage(AmqpAnnotatedMessage amqpMessage)
        {
            var serviceBusMessage = (ServiceBusReceivedMessage)Activator.CreateInstance(
                type: typeof(ServiceBusReceivedMessage),
                bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                args: new object[] { amqpMessage },
                culture: null,
                activationAttributes: null);

            return serviceBusMessage;
        }
    }
}
