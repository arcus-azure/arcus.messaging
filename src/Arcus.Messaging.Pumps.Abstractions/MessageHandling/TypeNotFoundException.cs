using System;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling 
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
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
        public TypeNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeNotFoundException"/> class.
        /// </summary>
        public TypeNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}