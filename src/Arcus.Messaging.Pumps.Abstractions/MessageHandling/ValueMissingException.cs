using System;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Exception thrown when a specific value of a type cannot be found during reflected selection.
    /// </summary>
    [Serializable]
    public class ValueMissingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotFoundException"/> class.
        /// </summary>
        public ValueMissingException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueMissingException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        public ValueMissingException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ValueMissingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
