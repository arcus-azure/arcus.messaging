using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Tests.Integration.Fixture.Logging
{
    public class InMemoryTelemetryChannel : ITelemetryChannel
    {
        private readonly ConcurrentStack<ITelemetry> _telemetries = new ConcurrentStack<ITelemetry>();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryTelemetryChannel" /> class.
        /// </summary>
        public InMemoryTelemetryChannel(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public ITelemetry[] Telemetries => _telemetries.ToArray();
        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; }

        public void Send(ITelemetry item)
        {
            switch (item)
            {
                case RequestTelemetry r:
                    _logger.LogTrace("Received {TelemetryType} telemetry (Name: {RequestName}) on the in-emory Telemetry channel", nameof(RequestTelemetry), r.Name);
                    break;

                case DependencyTelemetry d:
                    _logger.LogTrace("Received {TelemetryType} telemetry (Type: {DependencyType}) on the in-memory Telemetry channel", nameof(DependencyTelemetry), d.Type);
                    break;
            }

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
