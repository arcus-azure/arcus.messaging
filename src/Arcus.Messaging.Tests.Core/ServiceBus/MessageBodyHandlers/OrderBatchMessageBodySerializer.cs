using System;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageBodyHandlers
{
    public class OrderBatchMessageBodySerializer : IMessageBodyDeserializer
    {
        /// <summary>
        /// Tries to deserialize the incoming <paramref name="messageBody"/> to a message instance.
        /// </summary>
        /// <param name="messageBody">The incoming message body.</param>
        /// <returns>
        ///     A message result that either represents a successful or faulted deserialization result.
        /// </returns>
        public Task<MessageBodyResult> DeserializeMessageAsync(BinaryData messageBody)
        {
            var order = JsonConvert.DeserializeObject<Order>(messageBody.IsEmpty ? string.Empty : messageBody.ToString());

            if (order is null)
            {
                return Task.FromResult(MessageBodyResult.Failure("Cannot deserialize incoming message to an 'Order', so can't use 'Order'"));
            }

            return Task.FromResult(MessageBodyResult.Success(new OrderBatch { Orders = new[] { order } }));
        }
    }
}
