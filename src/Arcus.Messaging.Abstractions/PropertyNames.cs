using System;

#pragma warning disable S1133 // Deprecated functionality will be removed in v3.0.

namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Represents general property names within contextual information of processing messages.
    /// </summary>
    public static class PropertyNames
    {
        /// <summary>
        /// Gets the default context property name to get the operation ID of the parent operation.
        /// </summary>
        [Obsolete("Will be removed in v3.0 as the property name is only used in the deprecated 'Hierarchical' correlation format")]
        public const string OperationParentId = "Operation-Parent-Id";

        /// <summary>
        /// Gets the context property name to get the encoding that was used on the message.
        /// </summary>
        public const string Encoding = "Message-Encoding";
    }
}