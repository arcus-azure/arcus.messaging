using System;
using System.Collections.Generic;
using GuardNet;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents the contextual information concerning a message that will be processed by a message pump.
    /// </summary>
    public class MessageContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageContext"/> class.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message.</param>
        /// <param name="properties">The contextual properties provided on the message.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="messageId"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="properties"/> is <c>null</c>.</exception>
        public MessageContext(string messageId, IDictionary<string, object> properties)
        {
            Guard.NotNullOrEmpty(messageId, nameof(messageId));
            Guard.NotNull(properties, nameof(properties));

            MessageId = messageId;
            Properties = properties;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageContext"/> class.
        /// </summary>
        /// <param name="messageId">The unique identifier of the message.</param>
        /// <param name="jobId">The unique identifier of the message pump that processes the message.</param>
        /// <param name="properties">The contextual properties provided on the message.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="messageId"/> or the <paramref name="jobId"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="properties"/> is <c>null</c>.</exception>
        public MessageContext(string messageId, string jobId, IDictionary<string, object> properties)
        {
            Guard.NotNullOrEmpty(messageId, nameof(messageId));
            Guard.NotNullOrWhitespace(jobId, nameof(jobId));
            Guard.NotNull(properties, nameof(properties));

            MessageId = messageId;
            JobId = jobId;
            Properties = properties;
        }

        /// <summary>
        /// Gets unique identifier of the message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// Gets the unique ID on which message pump this message was processed.
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Gets the contextual properties provided on the message.
        /// </summary>
        public IDictionary<string, object> Properties { get; }
    }
}