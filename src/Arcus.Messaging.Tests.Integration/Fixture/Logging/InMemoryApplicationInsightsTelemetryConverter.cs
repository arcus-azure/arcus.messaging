using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Converters;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace Arcus.Messaging.Tests.Integration.Fixture.Logging
{
    public class InMemoryApplicationInsightsTelemetryConverter : TelemetryConverterBase
    {
        private readonly ApplicationInsightsTelemetryConverter _telemetryConverter;
        private readonly ConcurrentStack<ITelemetry> _telemetries = new ConcurrentStack<ITelemetry>();
        private readonly ConcurrentStack<LogEvent> _logEvents = new ConcurrentStack<LogEvent>();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryApplicationInsightsTelemetryConverter" /> class.
        /// </summary>
        public InMemoryApplicationInsightsTelemetryConverter(ILogger logger = null)
        {
            _telemetryConverter = ApplicationInsightsTelemetryConverter.Create();
            _logger = logger ?? NullLogger.Instance;
        }

        public ITelemetry[] Telemetries => _telemetries.ToArray();
        public LogEvent[] LogEvents => _logEvents.ToArray();

        public override IEnumerable<ITelemetry> Convert(LogEvent logEvent, IFormatProvider formatProvider)
        {
            _logEvents.Push(logEvent);

            IEnumerable<ITelemetry> telemetries = _telemetryConverter.Convert(logEvent, formatProvider);
            foreach (ITelemetry telemetry in telemetries)
            {
                switch (telemetry)
                {
                    case RequestTelemetry r:
                        _logger.LogTrace("Received {TelemetryType} telemetry (Name: {RequestName}, Transaction ID: {TransactionId}) in the in-memory Serilog sink", nameof(RequestTelemetry), r.Name, r.Context.Operation.Id);
                        break;

                    case DependencyTelemetry d:
                        _logger.LogTrace("Received {TelemetryType} telemetry (Type: {DependencyType}, Transaction ID: {TransactionId}) in the in-memory Serilog sink", nameof(DependencyTelemetry), d.Type, d.Context.Operation.Id);
                        break;
                }

                _telemetries.Push(telemetry);
            }

            return Enumerable.Empty<ITelemetry>();
        }
    }
}
