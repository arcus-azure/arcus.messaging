using System.Text;
using Arcus.Messaging.Abstractions;

namespace Arcus.Messaging.ServiceBus.Core.Extensions
{
    /// <summary>
    /// Extensions on the <see cref="MessageContext"/> to make accessing values more dev-friendly.
    /// </summary>
    public static class MessageContextExtensions
    {
        /// <summary>
        /// Gets the encoding property value in the messaging context, using UTF-8 as default if no such encoding value can be retrieved.
        /// </summary>
        /// <param name="messageContext">The context of the message.</param>
        public static Encoding GetMessageEncodingProperty(this MessageContext messageContext)
        {
            if (messageContext.Properties.TryGetValue(PropertyNames.Encoding, out object annotatedEncoding))
            {
                try
                {
                    return Encoding.GetEncoding(annotatedEncoding.ToString());
                }
                catch
                {
                    // Ignore.
                }
            }

            return Encoding.UTF8;
        }
    }
}
