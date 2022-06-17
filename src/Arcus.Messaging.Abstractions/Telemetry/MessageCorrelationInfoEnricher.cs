using System;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using GuardNet;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.Messaging.Abstractions.Telemetry
{
    /// <summary>
    /// Logger enrichment of the <see cref="MessageCorrelationInfo" /> model.
    /// </summary>
    public class MessageCorrelationInfoEnricher : ILogEventEnricher
    {
        private readonly ICorrelationInfoAccessor<MessageCorrelationInfo> _correlationInfoAccessor;
        private readonly MessageCorrelationInfo _messageCorrelationInfo;
        private const string CycleId = "CycleId";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Arcus.Observability.Telemetry.Serilog.Enrichers.CorrelationInfoEnricher`1" /> class.
        /// </summary>
        /// <param name="correlationInfoAccessor">The accessor implementation for the custom <see cref="T:Arcus.Observability.Correlation.CorrelationInfo" /> model.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="correlationInfoAccessor"/> is <c>null</c>.</exception>
        [Obsolete("Use the constructor overload with the message correlation model instead")]
        public MessageCorrelationInfoEnricher(ICorrelationInfoAccessor<MessageCorrelationInfo> correlationInfoAccessor)
        {
            Guard.NotNull(correlationInfoAccessor, nameof(correlationInfoAccessor), "Requires a correlation accessor to retrieve the correlation information that needs to be enriched on the log events");
            _correlationInfoAccessor = correlationInfoAccessor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCorrelationInfoEnricher" /> class.
        /// </summary>
        /// <param name="messageCorrelationInfo">The current message correlation instance that should be enriched on the log events.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageCorrelationInfo"/> is <c>null</c>.</exception>
        public MessageCorrelationInfoEnricher(MessageCorrelationInfo messageCorrelationInfo)
        {
            Guard.NotNull(messageCorrelationInfo, nameof(messageCorrelationInfo), "Requires a message correlation instance to enrich the log events");
            _messageCorrelationInfo = messageCorrelationInfo;
        }

        /// <summary>
        /// Enrich the log event.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            MessageCorrelationInfo correlation = DetermineMessageCorrelationInfo();
            if (correlation is null)
            {
                return;
            }

            AddPropertyIfAbsent(ContextProperties.Correlation.OperationId, correlation.OperationId, logEvent, propertyFactory);
            AddPropertyIfAbsent(ContextProperties.Correlation.TransactionId, correlation.TransactionId, logEvent, propertyFactory);
            AddPropertyIfAbsent(ContextProperties.Correlation.OperationParentId, correlation.OperationParentId, logEvent, propertyFactory);
            AddPropertyIfAbsent(CycleId, correlation.CycleId, logEvent, propertyFactory);
        }

        private MessageCorrelationInfo DetermineMessageCorrelationInfo()
        {
            if (_messageCorrelationInfo != null)
            {
                return _messageCorrelationInfo;
            }

            MessageCorrelationInfo correlation = _correlationInfoAccessor?.GetCorrelationInfo();
            return correlation;
        }

        private static void AddPropertyIfAbsent(string propertyName, string propertyValue, LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (!string.IsNullOrEmpty(propertyValue))
            {
                LogEventProperty property = propertyFactory.CreateProperty(propertyName, propertyValue);
                logEvent.AddPropertyIfAbsent(property);
            }
        }
    }
}
