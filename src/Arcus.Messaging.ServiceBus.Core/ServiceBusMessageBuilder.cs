using System;
using System.Collections.Generic;
using System.Text;
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.ServiceBus
{
    /// <summary>
    /// Represents a builder instance to create <see cref="ServiceBusMessage"/> instances in different ways.
    /// </summary>
    public class ServiceBusMessageBuilder
    {
        private readonly object _messageBody;
        private readonly Encoding _encoding;
        private KeyValuePair<string, object> _transactionIdProperty, _operationParentIdProperty, _operationIdProperty;

        private ServiceBusMessageBuilder(object messageBody, Encoding encoding)
        {
            _messageBody = messageBody;
            _encoding = encoding;
        }

        /// <summary>
        /// Starts a new <see cref="ServiceBusMessageBuilder"/> to create a new <see cref="ServiceBusMessage"/> from a given <paramref name="messageBody"/>.
        /// </summary>
        /// <param name="messageBody">The message body that will be serialized as the body of the <see cref="ServiceBusMessage"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageBody"/> is <c>null</c>.</exception>
        public static ServiceBusMessageBuilder CreateForBody(object messageBody)
        {
            Guard.NotNull(messageBody, nameof(messageBody), "Requires a message body to include in the to-be-created Azure Service Bus message");
            return new ServiceBusMessageBuilder(messageBody, Encoding.UTF8);
        }

        /// <summary>
        /// Starts a new <see cref="ServiceBusMessageBuilder"/> to create a new <see cref="ServiceBusMessage"/> from a given <paramref name="messageBody"/>.
        /// </summary>
        /// <param name="messageBody">The message body that will be serialized as the body of the <see cref="ServiceBusMessage"/>.</param>
        /// <param name="encoding">The encoding in which the <paramref name="messageBody"/> should be included in the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageBody"/> or the <paramref name="encoding"/> is <c>null</c>.</exception>
        public static ServiceBusMessageBuilder CreateForBody(object messageBody, Encoding encoding)
        {
            Guard.NotNull(messageBody, nameof(messageBody), "Requires a message body to include in the to-be-created Azure Service Bus message");
            Guard.NotNull(encoding, nameof(encoding), "Requires an encoding instance to encode the passed-in message body so it can be included in the Azure Service Bus message");
            
            return new ServiceBusMessageBuilder(messageBody, encoding);
        }

        /// <summary>
        /// Adds an <paramref name="operationId"/> as the <see cref="ServiceBusMessage.CorrelationId"/> to the <see cref="ServiceBusMessage"/>.
        /// </summary>
        /// <param name="operationId">The unique identifier for this operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="operationId"/> is blank.</exception>
        public ServiceBusMessageBuilder WithOperationId(string operationId)
        {
            Guard.NotNullOrWhitespace(operationId, nameof(operationId), "Requires a non-blank operation ID for the Azure Service Bus message");

            _operationIdProperty = new KeyValuePair<string, object>(null, operationId);
            return this;
        }

        /// <summary>
        /// Adds an <paramref name="operationId"/> as an application property in the <see cref="ServiceBusMessage.ApplicationProperties"/>.
        /// </summary>
        /// <param name="operationId">The unique identifier for this operation.</param>
        /// <param name="operationIdPropertyName">The name of the application property of the <paramref name="operationIdPropertyName"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="operationId"/> or the <paramref name="operationIdPropertyName"/> is blank.</exception>
        public ServiceBusMessageBuilder WithOperationId(string operationId, string operationIdPropertyName)
        {
            Guard.NotNullOrWhitespace(operationId, nameof(operationId), "Requires a non-blank operation ID for the Azure Service Bus message");
            Guard.NotNullOrWhitespace(operationIdPropertyName, nameof(operationIdPropertyName), "Requires a non-blank application property name to assign the operation ID to the Azure Service Bus message");

            _operationIdProperty = new KeyValuePair<string, object>(operationIdPropertyName, operationId);
            return this;
        }

        /// <summary>
        /// Adds a <paramref name="transactionId"/> as an application property in <see cref="ServiceBusMessage.ApplicationProperties"/>
        /// with <see cref="PropertyNames.TransactionId"/> as key.
        /// </summary>
        /// <param name="transactionId">The unique identifier of the current transaction.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="transactionId"/> is blank.</exception>
        public ServiceBusMessageBuilder WithTransactionId(string transactionId)
        {
            Guard.NotNullOrWhitespace(transactionId, nameof(transactionId), "Requires a non-blank transaction ID for the Azure Service Bus message");
            return WithTransactionId(transactionId, PropertyNames.TransactionId);
        }

        /// <summary>
        /// Adds a <paramref name="transactionId"/> as an application property in <see cref="ServiceBusMessage.ApplicationProperties"/>
        /// with <paramref name="transactionIdPropertyName"/> as key.
        /// </summary>
        /// <param name="transactionId">The unique identifier of the current transaction.</param>
        /// <param name="transactionIdPropertyName">The name of the application property for the <paramref name="transactionId"/>.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="transactionId"/> or the <paramref name="transactionIdPropertyName"/> is blank.</exception>
        public ServiceBusMessageBuilder WithTransactionId(string transactionId, string transactionIdPropertyName)
        {
            Guard.NotNullOrWhitespace(transactionId, nameof(transactionId), "Requires a non-blank transaction ID for the Azure Service Bus message");
            Guard.NotNullOrWhitespace(transactionIdPropertyName, nameof(transactionIdPropertyName), "Requires a non-blank property name of the transaction ID for the Azure Service Bus message");

            _transactionIdProperty = new KeyValuePair<string, object>(transactionIdPropertyName, transactionId);
            return this;
        }

        /// <summary>
        /// Adds a <paramref name="operationParentId"/> as an application property in <see cref="ServiceBusMessage.ApplicationProperties"/>
        /// with <see cref="PropertyNames.OperationParentId"/> as key.
        /// </summary>
        /// <param name="operationParentId">The unique identifier of the current parent operation.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="operationParentId"/> is blank.</exception>
        public ServiceBusMessageBuilder WithOperationParentId(string operationParentId)
        {
            Guard.NotNullOrWhitespace(operationParentId, nameof(operationParentId), "Requires a non-blank transaction ID for the Azure Service Bus message");
            return WithOperationParentId(operationParentId, PropertyNames.OperationParentId);
        }

        /// <summary>
        /// Adds a <paramref name="operationParentId"/> as an application property in <see cref="ServiceBusMessage.ApplicationProperties"/>
        /// with <paramref name="operationParentIdPropertyName"/> as key.
        /// </summary>
        /// <param name="operationParentId">The unique identifier of the current operation.</param>
        /// <param name="operationParentIdPropertyName">The name of the application property for the <paramref name="operationParentId"/>.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="operationParentId"/> or the <paramref name="operationParentIdPropertyName"/> is blank.</exception>
        public ServiceBusMessageBuilder WithOperationParentId(
            string operationParentId,
            string operationParentIdPropertyName)
        {
            Guard.NotNullOrWhitespace(operationParentId, nameof(operationParentId), "Requires a non-blank transaction ID for the Azure Service Bus message");
            Guard.NotNullOrWhitespace(operationParentIdPropertyName, nameof(operationParentIdPropertyName), "Requires a non-blank property name of the transaction ID for the Azure Service Bus message");

            _operationParentIdProperty = new KeyValuePair<string, object>(operationParentIdPropertyName, operationParentId);
            return this;
        }

        /// <summary>
        /// Creates an <see cref="ServiceBusMessage"/> instance based on the configured settings.
        /// </summary>
        public ServiceBusMessage Build()
        {
            string json = JsonConvert.SerializeObject(_messageBody);
            byte[] raw = _encoding.GetBytes(json);
            var message = new ServiceBusMessage(raw)
            {
                ApplicationProperties =
                {
                    { PropertyNames.ContentType, "application/json" },
                    { PropertyNames.Encoding, _encoding.WebName }
                }
            };

            if (_operationIdProperty.Key is null && _operationIdProperty.Value is not null)
            {
                message.CorrelationId = _operationIdProperty.Value?.ToString();
            } 
            else if (_operationIdProperty.Value is not null)
            {
                message.ApplicationProperties.Add(_operationIdProperty);
            }

            if (_transactionIdProperty.Key is not null)
            {
                message.ApplicationProperties.Add(_transactionIdProperty);
            }

            if (_operationParentIdProperty.Key is not null)
            {
                message.ApplicationProperties.Add(_operationParentIdProperty);
            }

            return message;
        }
    }
}