using System.Threading.Tasks;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling
{
    /// <summary>
    /// Represents a handler that provides a deserialization strategy for the incoming message during the message processing of the <see cref="MessagePump"/>.
    /// </summary>
    /// <seealso cref="IMessageHandler{TMessage,TMessageContext}"/>
    /// <seealso cref="MessagePump"/>
    public interface IMessageBodySerializer
    {
        /// <summary>
        /// Tries to deserialize the incoming <paramref name="messageBody"/> to a message instance.
        /// </summary>
        /// <param name="messageBody">The incoming message body.</param>
        /// <returns>
        ///     A message result that either represents a successful or faulted deserialization result.
        /// </returns>
        Task<MessageResult> DeserializeMessageAsync(string messageBody);
    }
}
