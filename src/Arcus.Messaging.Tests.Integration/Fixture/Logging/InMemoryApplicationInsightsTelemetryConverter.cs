using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Configuration;
using Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Converters;
using Microsoft.ApplicationInsights.Channel;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.Sinks.ApplicationInsights.TelemetryConverters;

namespace Arcus.Messaging.Tests.Integration.Fixture.Logging
{
    public class InMemoryApplicationInsightsTelemetryConverter : TelemetryConverterBase
    {
        private readonly ApplicationInsightsTelemetryConverter _telemetryConverter;
        private readonly ConcurrentStack<ITelemetry> _telemetries = new ConcurrentStack<ITelemetry>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryApplicationInsightsTelemetryConverter" /> class.
        /// </summary>
        public InMemoryApplicationInsightsTelemetryConverter()
        {
            _telemetryConverter = ApplicationInsightsTelemetryConverter.Create();
        }

        public ITelemetry[] Telemetries => _telemetries.ToArray();

        public override IEnumerable<ITelemetry> Convert(LogEvent logEvent, IFormatProvider formatProvider)
        {
            IEnumerable<ITelemetry> telemetries = _telemetryConverter.Convert(logEvent, formatProvider);
            foreach (ITelemetry telemetry in telemetries)
            {
                _telemetries.Push(telemetry);
            }

            return Enumerable.Empty<ITelemetry>();
        }
    }
}
