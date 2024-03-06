using System;
using System.Collections.Generic;
using System.Text;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Tests.Core;
using Arcus.Messaging.Tests.Core.Generators;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Azure.Messaging.ServiceBus;
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
            Order messagePayload = OrderGenerator.Generate();

            // Act
            ServiceBusMessage serviceBusMessage = ServiceBusMessageBuilder.CreateForBody(messagePayload).Build();

            // Assert
            Assert.NotNull(serviceBusMessage);
            Assert.Empty(serviceBusMessage.CorrelationId);
            IDictionary<string, object> userProperties = serviceBusMessage.ApplicationProperties;
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
            Order messagePayload = OrderGenerator.Generate();
            var operationId = Guid.NewGuid().ToString();

            // Act
            var serviceBusMessageBuilder = ServiceBusMessageBuilder.CreateForBody(messagePayload);
            serviceBusMessageBuilder.WithOperationId(operationId);
            ServiceBusMessage serviceBusMessage = serviceBusMessageBuilder.Build();

            // Assert
            Assert.NotNull(serviceBusMessage);
            Assert.Equal(operationId, serviceBusMessage.CorrelationId);
            IDictionary<string, object> userProperties = serviceBusMessage.ApplicationProperties;
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
            Order messagePayload = OrderGenerator.Generate();
            var expectedTransactionId = Guid.NewGuid().ToString();

            // Act
            var serviceBusMessageBuilder = ServiceBusMessageBuilder.CreateForBody(messagePayload);
            serviceBusMessageBuilder.WithTransactionId(transactionId: expectedTransactionId);
            ServiceBusMessage serviceBusMessage = serviceBusMessageBuilder.Build();

            // Assert
            Assert.NotNull(serviceBusMessage);
            Assert.Empty(serviceBusMessage.CorrelationId);
            IDictionary<string, object> userProperties = serviceBusMessage.ApplicationProperties;
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
            Order originalMessagePayload = OrderGenerator.Generate();
            Encoding expectedEncoding = Encoding.ASCII;

            // Act
            var serviceBusMessageBuilder = ServiceBusMessageBuilder.CreateForBody(originalMessagePayload, encoding: expectedEncoding);
            ServiceBusMessage serviceBusMessage = serviceBusMessageBuilder.Build();

            // Assert
            Assert.NotNull(serviceBusMessage);
            Assert.Empty(serviceBusMessage.CorrelationId);
            IDictionary<string, object> userProperties = serviceBusMessage.ApplicationProperties;
            Assert.True(userProperties.ContainsKey(PropertyNames.ContentType));
            Assert.True(userProperties.ContainsKey(PropertyNames.Encoding));
            Assert.False(userProperties.ContainsKey(PropertyNames.TransactionId));
            ArcusAssert.MatchesDictionaryEntry(PropertyNames.ContentType, ExpectedDefaultContentType, userProperties);
            AssertMessagePayload(serviceBusMessage, userProperties, expectedEncoding, originalMessagePayload);
        }

        private static void AssertMessagePayload(
            ServiceBusMessage serviceBusMessage,
            IDictionary<string, object> messageProperties,
            string rawEncoding,
            Order originalMessagePayload)
        {
            var encoding = Encoding.GetEncoding(rawEncoding);
            AssertMessagePayload(serviceBusMessage, messageProperties, encoding, originalMessagePayload);
        }

        private static void AssertMessagePayload(
            ServiceBusMessage serviceBusMessage,
            IDictionary<string, object> messageProperties,
            Encoding expectedEncoding,
            Order originalMessagePayload)
        {
            object rawEncoding = messageProperties[PropertyNames.Encoding];
            Assert.Equal(expectedEncoding.WebName, rawEncoding);
            var encoding = Encoding.GetEncoding(rawEncoding.ToString());
            string rawMessageBody = encoding.GetString(serviceBusMessage.Body);
            var messagePayload = JsonConvert.DeserializeObject<Order>(rawMessageBody);
            Assert.NotNull(messagePayload);
            Assert.Equal(originalMessagePayload.Id, messagePayload.Id);
        }
    }
}