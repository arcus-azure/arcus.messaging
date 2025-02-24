using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace Arcus.Messaging.Abstractions
{
    /// <summary>
    /// Extensions on the <see cref="MessageContext"/> to make accessing values more dev-friendly.
    /// </summary>
    public static class MessageContextExtensions
    {
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// Gets the encoding property value in the messaging context, using UTF-8 as default if no such encoding value can be retrieved.
        /// </summary>
        /// <param name="messageContext">The context of the message.</param>
        [Obsolete("Will be removed in v3.0, please use the " + nameof(PropertyNames.Encoding) + " to extract the encoding yourself from the message context")]
        public static Encoding GetMessageEncodingProperty(this MessageContext messageContext)
        {
            Encoding encoding = GetMessageEncodingProperty(messageContext, NullLogger.Instance);
            return encoding;
        }

        /// <summary>
        /// Gets the encoding property value in the messaging context, using UTF-8 as default if no such encoding value can be retrieved.
        /// </summary>
        /// <param name="messageContext">The context of the message.</param>
        /// <param name="logger"></param>
        [Obsolete("Will be removed in v3.0, please use the " + nameof(PropertyNames.Encoding) + " to extract the encoding yourself from the message context")]
        public static Encoding GetMessageEncodingProperty(this MessageContext messageContext, ILogger logger)
        {
            if (messageContext is null)
            {
                throw new ArgumentNullException(nameof(messageContext));
            }

            logger ??= NullLogger.Instance;

            if (messageContext.Properties.TryGetValue(PropertyNames.Encoding, out object annotatedEncoding))
            {
                try
                {
                    return Encoding.GetEncoding(annotatedEncoding.ToString());
                }
                catch (Exception ex)
                {
                    logger.LogCritical(
                        ex, "Unable to determine encoding with name '{Encoding}'. Falling back to {FallbackEncoding}.",
                        annotatedEncoding.ToString(), DefaultEncoding.WebName);
                }
            }

            return DefaultEncoding;
        }
    }
}
