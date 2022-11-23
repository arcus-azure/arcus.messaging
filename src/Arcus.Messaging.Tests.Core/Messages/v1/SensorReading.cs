using System;

namespace Arcus.Messaging.Tests.Core.Messages.v1
{
    public class SensorReading
    {
        public string SensorId { get; set; }
        public double SensorValue { get; set; }
        public SensorStatus SensorStatus { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
