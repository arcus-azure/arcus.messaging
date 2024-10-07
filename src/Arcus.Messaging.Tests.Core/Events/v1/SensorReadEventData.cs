using Arcus.Messaging.Abstractions;

namespace Arcus.Messaging.Tests.Core.Events.v1
{
    public class SensorReadEventData
    {
        public string SensorId { get; set; }
        public double SensorValue { get; set; }
        public MessageCorrelationInfo CorrelationInfo { get; set; }
    }
}
