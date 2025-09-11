using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageBodyHandlers
{
    public class OrderBatchMessageBodySerializer : IMessageBodySerializer
    {
        private readonly ILogger<OrderBatchMessageBodySerializer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBatchMessageBodySerializer"/> class.
        /// </summary>
        public OrderBatchMessageBodySerializer(ILogger<OrderBatchMessageBodySerializer> logger)
        {
            _logger = logger ?? NullLogger<OrderBatchMessageBodySerializer>.Instance;
        }

        /// <summary>
        /// Tries to deserialize the incoming <paramref name="messageBody"/> to a message instance.
        /// </summary>
        /// <param name="messageBody">The incoming message body.</param>
        /// <returns>
        ///     A message result that either represents a successful or faulted deserialization result.
        /// </returns>
        public Task<MessageResult> DeserializeMessageAsync(string messageBody)
        {
            var order = JsonConvert.DeserializeObject<Order>(messageBody);

            if (order is null)
            {
                return Task.FromResult(MessageResult.Failure("Cannot deserialize incoming message to an 'Order', so can't use 'Order'"));
            }

            return Task.FromResult(MessageResult.Success(new OrderBatch { Orders = new[] { order } }));
        }
    }
}
