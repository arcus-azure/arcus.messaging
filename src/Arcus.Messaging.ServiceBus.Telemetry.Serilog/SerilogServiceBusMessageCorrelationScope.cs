using System;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Arcus.Messaging.ServiceBus.Telemetry.Serilog
{
    /// <summary>
    /// Represents the Serilog-specific implementation of the <see cref="IServiceBusMessageCorrelationScope"/>
    /// to track via Serilog Azure Service bus requests in Azure Application Insights.
    /// </summary>
    internal class SerilogServiceBusMessageCorrelationScope : IServiceBusMessageCorrelationScope
    {
        private readonly TelemetryClient _client;
        private readonly SerilogMessageCorrelationOptions _serilogOptions;
        private readonly ILogger<SerilogServiceBusMessageCorrelationScope> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogServiceBusMessageCorrelationScope"/> class.
        /// </summary>
        internal SerilogServiceBusMessageCorrelationScope(
            TelemetryClient client,
            SerilogMessageCorrelationOptions serilogOptions,
            ILogger<SerilogServiceBusMessageCorrelationScope> logger)
        {
            _client = client;
            _serilogOptions = serilogOptions;
            _logger = logger;
        }

        /// <summary>
        /// Starts a new Azure Service bus request operation on the telemetry system.
        /// </summary>
        /// <param name="messageContext">The message context for the currently received Azure Service bus message.</param>
        /// <param name="options">The user-configurable options to manipulate the telemetry.</param>
        public MessageOperationResult StartOperation(AzureServiceBusMessageContext messageContext, MessageTelemetryOptions options)
        {
            (string transactionId, string operationParentId) = messageContext.Properties.GetTraceParent();

            var telemetry = new RequestTelemetry();
            telemetry.Context.Operation.Id = transactionId;
            telemetry.Context.Operation.ParentId = operationParentId;

            IOperationHolder<RequestTelemetry> operationHolder = _client.StartOperation(telemetry);
            var correlation = new MessageCorrelationInfo(operationHolder.Telemetry.Id, transactionId, operationParentId);

            IDisposable disposable = LogContext.Push(new SerilogMessageCorrelationInfoEnricher(correlation, _serilogOptions.Enricher));

            return new SerilogMessageOperationResult(correlation, stopOperation: (isSuccessful, startTime, duration) =>
            {
                _logger.LogServiceBusRequest(
                    messageContext.FullyQualifiedNamespace,
                    messageContext.EntityPath,
                    options.OperationName,
                    isSuccessful,
                    duration,
                    startTime,
                    messageContext.EntityType);

                _client.TelemetryConfiguration.DisableTelemetry = true;
                operationHolder.Dispose();
                _client.TelemetryConfiguration.DisableTelemetry = false;

                disposable.Dispose();
            });
        }

        private sealed class SerilogMessageOperationResult : MessageOperationResult
        {
            private readonly Action<bool, DateTimeOffset, TimeSpan> _stopOperation;

            internal SerilogMessageOperationResult(
                MessageCorrelationInfo correlation,
                Action<bool, DateTimeOffset, TimeSpan> stopOperation) : base(correlation)
            {
                _stopOperation = stopOperation;
            }

            /// <summary>
            /// Finalizes the tracked operation in the concrete telemetry system, based on the operation results.
            /// </summary>
            /// <param name="isSuccessful">The boolean flag to indicate whether the operation was successful.</param>
            /// <param name="startTime">The date when the operation started.</param>
            /// <param name="duration">The time it took for the operation to run.</param>
            protected override void StopOperation(bool isSuccessful, DateTimeOffset startTime, TimeSpan duration)
            {
                _stopOperation(isSuccessful, startTime, duration);
            }
        }
    }
}