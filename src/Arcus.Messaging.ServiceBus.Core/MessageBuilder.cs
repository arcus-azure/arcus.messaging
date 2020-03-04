using System;
using System.Text;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.ServiceBus
{
    /// <summary>
    /// Model to build <see cref="Message"/> models from a raw message body.
    /// </summary>
    public class MessageBuilder
    {
        private const string JsonContentType = "application/json";
        
        private readonly object _messageBody;
        private readonly Encoding _encoding;
        private readonly string _operationId, _transactionId;

        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        private MessageBuilder(object messageBody, Encoding encoding = null, string operationId = null, string transactionId = null)
        {
            Guard.NotNull(messageBody, nameof(messageBody));

            _messageBody = messageBody;
            _encoding = encoding ?? DefaultEncoding;
            _operationId = operationId;
            _transactionId = transactionId;
        }

        /// <summary>
        /// Creates a builder with a raw <paramref name="messageBody"/>.
        /// </summary>
        /// <param name="messageBody">The body in the <see cref="Message"/>.</param>
        public static MessageBuilder ForMessageBody(object messageBody)
        {
            Guard.NotNull(messageBody, nameof(messageBody));

            return new MessageBuilder(messageBody);
        }

        /// <summary>
        /// Creates a builder with a raw <paramref name="messageBody"/>.
        /// </summary>
        /// <param name="messageBody">The body in the <see cref="Message"/>.</param>
        /// <param name="encoding">The encoding to serialize the <paramref name="messageBody"/>.</param>
        public static MessageBuilder ForMessageBody(object messageBody, Encoding encoding)
        {
            Guard.NotNull(messageBody, nameof(messageBody));

            return new MessageBuilder(messageBody, encoding);
        }

        /// <summary>
        /// Adds a operation ID that corresponds with the <see cref="Message.CorrelationId"/>.
        /// </summary>
        /// <param name="operationId">The operation identifier.</param>
        public MessageBuilder WithOperationId(string operationId)
        {
            return new MessageBuilder(_messageBody, _encoding, operationId, _transactionId);
        }

        /// <summary>
        /// Adds a transaction ID that corresponds with a property in the <see cref="Message.UserProperties"/> with the name of <see cref="PropertyNames.TransactionId"/>.
        /// </summary>
        /// <param name="transactionId">The transaction identifier.</param>
        public MessageBuilder WithTransactionId(string transactionId)
        {
            return new MessageBuilder(_messageBody, _encoding, _operationId, transactionId);
        }

        /// <summary>
        /// Constructs a <see cref="Message"/> from the previously configured values.
        /// </summary>
        public Message Build()
        {
            string serializedMessageBody = JsonConvert.SerializeObject(_messageBody);
            byte[] rawMessage = _encoding.GetBytes(serializedMessageBody);

            var serviceBusMessage = new Message(rawMessage)
            {
                UserProperties =
                {
                    { PropertyNames.ContentType, JsonContentType },
                    { PropertyNames.Encoding, _encoding.WebName }
                },
                CorrelationId = _operationId
            };

            if (String.IsNullOrWhiteSpace(_transactionId) == false)
            {
                serviceBusMessage.UserProperties.Add(PropertyNames.TransactionId, _transactionId);
            }

            return serviceBusMessage;
        }
    }
}
