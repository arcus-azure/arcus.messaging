using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Messaging.Tests.Workers.EventHubs.Core.MessageBodySerializers
{
    public class SensorReadingBatchBodySerializer : IMessageBodySerializer
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorReadingBatchBodySerializer" /> class.
        /// </summary>
        public SensorReadingBatchBodySerializer(ILogger<SensorReadingBatchBodySerializer> logger)
        {
            _logger = logger;
        }

        public Task<MessageResult> DeserializeMessageAsync(string messageBody)
        {
            _logger.LogTrace("Start deserializing to an 'SensorReading'...");
            var reading = JsonConvert.DeserializeObject<SensorReading>(messageBody);
            
            if (reading is null)
            {
                _logger.LogError("Cannot deserialize incoming message to an 'SensorReading', so can't use 'SensorReadingBatch'");
                return Task.FromResult(MessageResult.Failure("Cannot deserialize incoming message to an 'SensorReading', so can't use 'SensorReadingBatch'"));
            }

            _logger.LogInformation("Deserialized to an 'SensorReadingBatch', using 'SensorReading'");
            return Task.FromResult(MessageResult.Success(new SensorReadingBatch { Readings = new [] { reading } }));
        }
    }
}
