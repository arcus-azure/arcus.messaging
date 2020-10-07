using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.MessageBodyHandlers
{
    public class OrderBatchMessageBodyHandler : IMessageBodyHandler
    {
        private readonly ILogger<OrderBatchMessageBodyHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBatchMessageBodyHandler"/> class.
        /// </summary>
        public OrderBatchMessageBodyHandler(ILogger<OrderBatchMessageBodyHandler> logger)
        {
            _logger = logger ?? NullLogger<OrderBatchMessageBodyHandler>.Instance;
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
            _logger.LogTrace("Start deserializing to an 'Order'...");
            var order = JsonConvert.DeserializeObject<Order>(messageBody);
            
            if (order is null)
            {
                _logger.LogError("Cannot deserialize incoming message to an 'Order', so can't use 'Order'");
                return Task.FromResult(MessageResult.Failure());
            }

            _logger.LogInformation("Deserialized to an 'Order', using 'Order'");
            return Task.FromResult(MessageResult.Success(new OrderBatch { Orders = new [] { order } }));
        }
    }
}
