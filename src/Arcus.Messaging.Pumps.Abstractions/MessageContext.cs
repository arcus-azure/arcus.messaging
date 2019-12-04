using System.Collections.Generic;
using GuardNet;

namespace Arcus.Messaging.Pumps.Abstractions
{
    /// <summary>
    ///     Contextual information concerning a message
    /// </summary>
    public class MessageContext
    {
        /// <summary>
        ///     Unique identifier of the message
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        ///     Contextual properties provided on the message
        /// </summary>
        public IDictionary<string, object> Properties { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="messageId">Unique identifier of the message</param>
        /// <param name="properties">Contextual properties provided on the message</param>
        public MessageContext(string messageId, IDictionary<string, object> properties)
        {
            Guard.NotNullOrEmpty(messageId, nameof(messageId));
            Guard.NotNull(properties, nameof(properties));

            MessageId = messageId;
            Properties = properties;
        }
    }
}