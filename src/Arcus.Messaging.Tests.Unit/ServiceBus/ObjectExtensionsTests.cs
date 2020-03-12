using System;
using System.Collections.Generic;
using System.Text;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.ServiceBus.Core.Extensions;
using Arcus.Messaging.Tests.Core;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.ServiceBus
{
    [Trait("Category", "Unit")]
    public class ObjectExtensionsTests
    {
        private const string ExpectedDefaultEncoding = "utf-8";
        private const string ExpectedDefaultContentType = "application/json";

        [Fact]
        public void WrapInServiceBusMessage_BasicWithoutOptions_ReturnsValidServiceBusMessage()
        {
            // Arrange
            var messagePayload = OrderGenerator.Generate();

            // Act
            var serviceBusMessage = messagePayload.AsServiceBusMessage();

            // Assert
            Assert.NotNull(serviceBusMessage);
            Assert.Null(serviceBusMessage.CorrelationId);
            var userProperties = serviceBusMessage.UserProperties;
            Assert.True(userProperties.ContainsKey(PropertyNames.ContentType));
            Assert.True(userProperties.ContainsKey(PropertyNames.Encoding));
            Assert.False(userProperties.ContainsKey(PropertyNames.TransactionId));
            ArcusAssert.MatchesDictionaryEntry(PropertyNames.ContentType, ExpectedDefaultContentType, userProperties);
            ArcusAssert.MatchesDictionaryEntry(PropertyNames.Encoding, ExpectedDefaultEncoding, userProperties);
            AssertMessagePayload(serviceBusMessage, userProperties, ExpectedDefaultEncoding, messagePayload);
        }

        [Fact]
        public void WrapInServiceBusMessage_BasicWithOperationId_ReturnsValidServiceBusMessageWithSpecifiedCorrelationId()
        {
            // Arrange
            var messagePayload = OrderGenerator.Generate();
            var operationId = Guid.NewGuid().ToString();

            // Act
            var serviceBusMessage = messagePayload.AsServiceBusMessage(operationId: operationId);

            // Assert
            Assert.NotNull(serviceBusMessage);
            Assert.Equal(operationId, serviceBusMessage.CorrelationId);
            var userProperties = serviceBusMessage.UserProperties;
            Assert.True(userProperties.ContainsKey(PropertyNames.ContentType));
            Assert.True(userProperties.ContainsKey(PropertyNames.Encoding));
            Assert.False(userProperties.ContainsKey(PropertyNames.TransactionId));
            ArcusAssert.MatchesDictionaryEntry(PropertyNames.ContentType, ExpectedDefaultContentType, userProperties);
            ArcusAssert.MatchesDictionaryEntry(PropertyNames.Encoding, ExpectedDefaultEncoding, userProperties);
            AssertMessagePayload(serviceBusMessage, userProperties, ExpectedDefaultEncoding, messagePayload);
        }

        [Fact]
        public void WrapInServiceBusMessage_BasicWithTransactionId_ReturnsValidServiceBusMessageWithSpecifiedTransactionId()
        {
            // Arrange
            var messagePayload = OrderGenerator.Generate();
            var expectedTransactionId = Guid.NewGuid().ToString();

            // Act
            var serviceBusMessage = messagePayload.AsServiceBusMessage(transactionId: expectedTransactionId);

            // Assert
            Assert.NotNull(serviceBusMessage);
            Assert.Null(serviceBusMessage.CorrelationId);
            var userProperties = serviceBusMessage.UserProperties;
            Assert.True(userProperties.ContainsKey(PropertyNames.ContentType));
            Assert.True(userProperties.ContainsKey(PropertyNames.Encoding));
            Assert.True(userProperties.ContainsKey(PropertyNames.TransactionId));
            ArcusAssert.MatchesDictionaryEntry(PropertyNames.ContentType, ExpectedDefaultContentType, userProperties);
            ArcusAssert.MatchesDictionaryEntry(PropertyNames.Encoding, ExpectedDefaultEncoding, userProperties);
            ArcusAssert.MatchesDictionaryEntry(PropertyNames.TransactionId, expectedTransactionId, userProperties);
            AssertMessagePayload(serviceBusMessage, userProperties, ExpectedDefaultEncoding, messagePayload);
        }

        [Fact]
        public void WrapInServiceBusMessage_BasicWithEncoding_ReturnsValidServiceBusMessageWithSpecifiedEncoding()
        {
            // Arrange
            var originalMessagePayload = OrderGenerator.Generate();
            var expectedEncoding = Encoding.ASCII;

            // Act
            var serviceBusMessage = originalMessagePayload.AsServiceBusMessage(encoding: expectedEncoding);

            // Assert
            Assert.NotNull(serviceBusMessage);
            Assert.Null(serviceBusMessage.CorrelationId);
            var userProperties = serviceBusMessage.UserProperties;
            Assert.True(userProperties.ContainsKey(PropertyNames.ContentType));
            Assert.True(userProperties.ContainsKey(PropertyNames.Encoding));
            Assert.False(userProperties.ContainsKey(PropertyNames.TransactionId));
            ArcusAssert.MatchesDictionaryEntry(PropertyNames.ContentType, ExpectedDefaultContentType, userProperties);
            AssertMessagePayload(serviceBusMessage, userProperties, expectedEncoding, originalMessagePayload);
        }

        private static void AssertMessagePayload(Message serviceBusMessage, IDictionary<string, object> messageProperties, string rawEncoding, Order originalMessagePayload)
        {
            var encoding = Encoding.GetEncoding(rawEncoding);
            AssertMessagePayload(serviceBusMessage, messageProperties, encoding, originalMessagePayload);
        }

        private static void AssertMessagePayload(Message serviceBusMessage, IDictionary<string, object> messageProperties, Encoding expectedEncoding,
             Order originalMessagePayload)
        {
            var rawEncoding = messageProperties[PropertyNames.Encoding];
            Assert.Equal(expectedEncoding.WebName, rawEncoding);
            var encoding = Encoding.GetEncoding(rawEncoding.ToString());
            var rawMessageBody = encoding.GetString(serviceBusMessage.Body);
            var messagePayload = JsonConvert.DeserializeObject<Order>(rawMessageBody);
            Assert.NotNull(messagePayload);
            Assert.Equal(originalMessagePayload.Id, messagePayload.Id);
        }
    }
}