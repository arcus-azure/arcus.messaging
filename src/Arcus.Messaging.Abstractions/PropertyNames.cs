using System;

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents general property names within contextual information of processing messages.
    /// </summary>
    public static class PropertyNames
    {
        /// <summary>
        /// Gets the context property name to get the encoding that was used on the message.
        /// </summary>
        public const string Encoding = "Message-Encoding";
    }
}