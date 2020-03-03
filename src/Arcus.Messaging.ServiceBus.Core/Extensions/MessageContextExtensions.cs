using System;
using System.Text;
using Arcus.Messaging.Abstractions;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.ServiceBus.Core.Extensions
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
        public static Encoding GetMessageEncodingProperty(this MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));

            Encoding encoding = GetMessageEncodingProperty(messageContext, NullLogger.Instance);
            return encoding;
        }

        /// <summary>
        /// Gets the encoding property value in the messaging context, using UTF-8 as default if no such encoding value can be retrieved.
        /// </summary>
        /// <param name="messageContext">The context of the message.</param>
        /// <param name="logger"></param>
        public static Encoding GetMessageEncodingProperty(this MessageContext messageContext, ILogger logger)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            logger = logger ?? NullLogger.Instance;

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
