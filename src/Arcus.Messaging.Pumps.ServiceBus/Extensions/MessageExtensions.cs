using GuardNet;
using Microsoft.Azure.ServiceBus;

namespace Arcus.Messaging.Pumps.ServiceBus.Extensions
{
    public static class MessageExtensions
    {
        /// <summary>
        ///     Gets the transaction id that is linked to this message
        /// </summary>
        /// <param name="message">Message to process</param>
        /// <returns>Transaction id for message</returns>
        public static string GetTransactionId(this Message message)
        {
            Guard.NotNull(message, nameof(message));

            return message.UserProperties.TryGetValue(PropertyNames.TransactionId, out object transactionId)
                ? transactionId.ToString()
                : string.Empty;
        }
    }
}