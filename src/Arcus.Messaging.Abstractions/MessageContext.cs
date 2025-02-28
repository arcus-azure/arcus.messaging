using System;
using System.Collections.Generic;

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
        [Obsolete("Will be removed in v3.0 as only job-linked message contexts are supported from now on")]
        public MessageContext(string messageId, IDictionary<string, object> properties)
        {
            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException("Requires a non-blank message ID", nameof(messageId));
            }

            MessageId = messageId;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
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
            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException("Requires a non-blank message ID", nameof(messageId));
            }

            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Requires a non-blank job ID", nameof(jobId));
            }

            MessageId = messageId;
            JobId = jobId;
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
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