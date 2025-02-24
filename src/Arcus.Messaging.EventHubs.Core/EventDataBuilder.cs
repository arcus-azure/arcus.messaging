using System;
using System.Collections.Generic;
using System.Text;
using Arcus.Messaging.Abstractions;
using Newtonsoft.Json;

#pragma warning disable CS0618 // All EventHubs-functionality will be removed anyway, so ignore deprecated correlation properties.

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.EventHubs
{
    /// <summary>
    /// Represents a builder instance to create <see cref="EventData"/> instances in different ways.
    /// </summary>
    public class EventDataBuilder
    {
        private readonly object _eventBody;
        private readonly Encoding _encoding;

        private KeyValuePair<string, object> _transactionIdProperty, _operationParentIdProperty, _operationIdProperty;

        private EventDataBuilder(object eventBody, Encoding encoding)
        {
            _eventBody = eventBody;
            _encoding = encoding;
        }

        /// <summary>
        /// Starts a new <see cref="EventDataBuilder"/> to create a new <see cref="EventData"/> from a given <paramref name="eventBody"/>.
        /// </summary>
        /// <param name="eventBody">The message body that will be serialized as the body of the <see cref="EventData"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventBody"/> or the is <c>null</c>.</exception>
        public static EventDataBuilder CreateForBody(object eventBody)
        {
            return CreateForBody(eventBody, Encoding.UTF8);
        }

        /// <summary>
        /// Starts a new <see cref="EventDataBuilder"/> to create a new <see cref="EventData"/> from a given <paramref name="eventBody"/>.
        /// </summary>
        /// <param name="eventBody">The message body that will be serialized as the body of the <see cref="EventData"/>.</param>
        /// <param name="encoding">The encoding in which the <paramref name="eventBody"/> should be included in the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="eventBody"/> or the <paramref name="encoding"/> is <c>null</c>.</exception>
        public static EventDataBuilder CreateForBody(object eventBody, Encoding encoding)
        {
            return new EventDataBuilder(
                eventBody ?? throw new ArgumentNullException(nameof(eventBody)),
                encoding ?? throw new ArgumentNullException(nameof(encoding)));
        }

        /// <summary>
        /// Adds an <paramref name="operationId"/> as the <see cref="EventData.CorrelationId"/> to the <see cref="EventData"/>.
        /// </summary>
        /// <param name="operationId">The unique identifier for this operation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="operationId"/> is blank.</exception>
        public EventDataBuilder WithOperationId(string operationId)
        {
            if (operationId is not null)
            {
                _operationIdProperty = new KeyValuePair<string, object>(null, operationId);
            }

            return this;
        }

        /// <summary>
        /// Adds an <paramref name="operationId"/> as an application property in the <see cref="EventData.Properties"/>.
        /// </summary>
        /// <param name="operationId">The unique identifier for this operation.</param>
        /// <param name="operationIdPropertyName">The custom name that must be given to the application property that contains the <paramref name="operationId"/>.</param>
        /// <remarks>
        ///     <para>When no <paramref name="operationId"/> is specified, no operation ID will be set on the <see cref="EventData"/>;</para>
        ///     <para>when no <paramref name="operationIdPropertyName"/> is specified, the default location <see cref="EventData.CorrelationId"/> will be used to set the operation ID.</para>
        /// </remarks>
        public EventDataBuilder WithOperationId(string operationId, string operationIdPropertyName)
        {
            if (operationId is not null)
            {
                _operationIdProperty = new KeyValuePair<string, object>(operationIdPropertyName, operationId);
            }

            return this;
        }

        /// <summary>
        /// Adds a <paramref name="transactionId"/> as an application property in <see cref="EventData.Properties"/>
        /// with <see cref="PropertyNames.TransactionId"/> as key.
        /// </summary>
        /// <param name="transactionId">The unique identifier of the current transaction.</param>
        /// <remarks>When no <paramref name="transactionId"/> is specified, no transaction ID will be set on the <see cref="EventData"/>.</remarks>
        public EventDataBuilder WithTransactionId(string transactionId)
        {
            return WithTransactionId(transactionId, PropertyNames.TransactionId);
        }

        /// <summary>
        /// Adds a <paramref name="transactionId"/> as an application property in <see cref="EventData.Properties"/>
        /// with <paramref name="transactionIdPropertyName"/> as key.
        /// </summary>
        /// <param name="transactionId">The unique identifier of the current transaction.</param>
        /// <param name="transactionIdPropertyName">The custom name that must be given to the application property that contains the <paramref name="transactionId"/>.</param>
        /// <remarks>
        ///     <para>When no <paramref name="transactionId"/> is specified, no transaction ID will be set on the <see cref="EventData"/>;</para>
        ///     <para>when no <paramref name="transactionIdPropertyName"/> is specified, the default <see cref="PropertyNames.TransactionId"/> application property name will be used.</para>
        /// </remarks>
        public EventDataBuilder WithTransactionId(string transactionId, string transactionIdPropertyName)
        {
            if (transactionId is not null)
            {
                _transactionIdProperty = new KeyValuePair<string, object>(
                    transactionIdPropertyName ?? PropertyNames.TransactionId,
                    transactionId);
            }

            return this;
        }

        /// <summary>
        /// Adds a <paramref name="operationParentId"/> as an application property in <see cref="EventData.Properties"/>
        /// with <see cref="PropertyNames.OperationParentId"/> as key.
        /// </summary>
        /// <param name="operationParentId">The unique identifier of the current parent operation.</param>
        /// <remarks>When no <paramref name="operationParentId"/> is specified, no operation parent ID will be set on the <see cref="EventData"/>.</remarks>
        public EventDataBuilder WithOperationParentId(string operationParentId)
        {
            return WithOperationParentId(operationParentId, PropertyNames.OperationParentId);
        }

        /// <summary>
        /// Adds a <paramref name="operationParentId"/> as an application property in <see cref="EventData.Properties"/>
        /// with <paramref name="operationParentIdPropertyName"/> as key.
        /// </summary>
        /// <param name="operationParentId">The unique identifier of the current operation.</param>
        /// <param name="operationParentIdPropertyName">The custom name that must be given to the application property that contains the <paramref name="operationParentId"/>.</param>
        /// <remarks>
        ///     <para>When no <paramref name="operationParentId"/> is specified, no operation parent ID will be set on the <see cref="EventData"/>;</para>
        ///     <para>when no <paramref name="operationParentIdPropertyName"/> is specified, the default <see cref="PropertyNames.OperationParentId"/> application property name will be used.</para>
        /// </remarks>
        public EventDataBuilder WithOperationParentId(
            string operationParentId,
            string operationParentIdPropertyName)
        {
            if (operationParentId is not null)
            {
                _operationParentIdProperty = new KeyValuePair<string, object>(
                    operationParentIdPropertyName ?? PropertyNames.OperationParentId,
                    operationParentId);
            }

            return this;
        }

        /// <summary>
        /// Creates an <see cref="EventData"/> instance based on the configured settings.
        /// </summary>
        public EventData Build()
        {
            string json = JsonConvert.SerializeObject(_eventBody);
            byte[] raw = _encoding.GetBytes(json);
            var eventData = new EventData(raw)
            {
                Properties =
                {
                    { PropertyNames.ContentType, "application/json" },
                    { PropertyNames.Encoding, _encoding.WebName }
                }
            };

            if (_operationIdProperty.Key is null && _operationIdProperty.Value is not null)
            {
                eventData.CorrelationId = _operationIdProperty.Value?.ToString();
            }
            else if (_operationIdProperty.Value is not null)
            {
                eventData.Properties.Add(_operationIdProperty);
            }

            if (_transactionIdProperty.Key is not null)
            {
                eventData.Properties.Add(_transactionIdProperty);
            }

            if (_operationParentIdProperty.Key is not null)
            {
                eventData.Properties.Add(_operationParentIdProperty);
            }

            return eventData;
        }
    }
}
