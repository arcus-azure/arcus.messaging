using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Tests.Workers.MessageBodyHandlers
{
    public class PassThruMessageBodySerializer : IMessageBodySerializer
    {
        /// <summary>
        /// Tries to deserialize the incoming <paramref name="messageBody"/> to a message instance.
        /// </summary>
        /// <param name="messageBody">The incoming message body.</param>
        /// <returns>
        ///     A message result that either represents a successful or faulted deserialization result.
        /// </returns>
        public Task<MessageResult> DeserializeMessageAsync(string messageBody)
        {
            return Task.FromResult(MessageResult.Success(new Random()));
        }
    }
}
