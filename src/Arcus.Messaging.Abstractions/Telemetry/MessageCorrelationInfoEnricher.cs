using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Serilog.Enrichers;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.Messaging.Abstractions.Telemetry
{
    /// <summary>
    /// Logger enrichment of the <see cref="MessageCorrelationInfo" /> model.
    /// </summary>
    public class MessageCorrelationInfoEnricher : CorrelationInfoEnricher<MessageCorrelationInfo>
    {
        private const string CycleId = "CycleId";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Arcus.Observability.Telemetry.Serilog.Enrichers.CorrelationInfoEnricher`1" /> class.
        /// </summary>
        /// <param name="correlationInfoAccessor">The accessor implementation for the custom <see cref="T:Arcus.Observability.Correlation.CorrelationInfo" /> model.</param>
        public MessageCorrelationInfoEnricher(ICorrelationInfoAccessor<MessageCorrelationInfo> correlationInfoAccessor) 
            : base(correlationInfoAccessor)
        {
        }

        /// <summary>
        /// Enrich the <paramref name="logEvent" /> with the given <paramref name="correlationInfo" /> model.
        /// </summary>
        /// <param name="logEvent">The log event to enrich with correlation information.</param>
        /// <param name="propertyFactory">The log property factory to create log properties with correlation information.</param>
        /// <param name="correlationInfo">The correlation model that contains the current correlation information.</param>
        protected override void EnrichCorrelationInfo(
            LogEvent logEvent,
            ILogEventPropertyFactory propertyFactory,
            MessageCorrelationInfo correlationInfo)
        {
            base.EnrichCorrelationInfo(logEvent, propertyFactory, correlationInfo);

            if (!string.IsNullOrEmpty(correlationInfo.CycleId))
            {
                LogEventProperty property = propertyFactory.CreateProperty(CycleId, correlationInfo.CycleId);
                logEvent.AddPropertyIfAbsent(property);
            }
        }
    }
}
