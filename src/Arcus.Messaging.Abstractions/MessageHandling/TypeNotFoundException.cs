using System;

namespace Arcus.Messaging.Abstractions.MessageHandling
{
    /// <summary>
    /// Exception thrown when a specific type cannot be found during reflected selection.
    /// </summary>
    [Serializable]
    [Obsolete("Will be removed in v3.0 as no reflection is included anymore in message routing, which means no need for this custom exception")]
    public class TypeNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotFoundException"/> class.
        /// </summary>
        public TypeNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        public TypeNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TypeNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}