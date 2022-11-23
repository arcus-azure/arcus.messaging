using Arcus.Messaging.Tests.Core.Messages.v1;
using Bogus;

namespace Arcus.Messaging.Tests.Core.Generators
{
    public static class SensorReadingGenerator
    {
        public static SensorReading Generate()
        {
            return new Faker<SensorReading>()
                .RuleFor(r => r.SensorId, f => f.Random.Guid().ToString())
                .RuleFor(r => r.SensorValue, f => f.Random.Double())
                .RuleFor(r => r.SensorStatus, f => f.PickRandom<SensorStatus>())
                .RuleFor(r => r.Timestamp, f => f.Date.RecentOffset())
                .Generate();
        }
    }
}
