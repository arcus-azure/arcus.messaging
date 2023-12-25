using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace Arcus.Messaging.Tests.Integration.Fixture.Logging
{
    public class InMemoryTelemetryChannel : ITelemetryChannel
    {
        private readonly ConcurrentStack<ITelemetry> _telemetries = new ConcurrentStack<ITelemetry>();

        public ITelemetry[] Telemetries => _telemetries.ToArray();
        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; }

        public void Send(ITelemetry item)
        {
            _telemetries.Push(item);
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}
