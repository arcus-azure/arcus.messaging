using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.Telemetry;
using Arcus.Messaging.Abstractions.Telemetry;
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
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(serilogOptions);
            ArgumentNullException.ThrowIfNull(logger);

            _client = client;
            _serilogOptions = serilogOptions;
            _logger = logger;
        }

        /// <summary>
        /// Starts a new Azure Service bus request operation on the telemetry system.
        /// </summary>
        /// <param name="messageContext">The message context for the currently received Azure Service bus message.</param>
        /// <param name="options">The user-configurable options to manipulate the telemetry.</param>
        public MessageOperationResult StartOperation(ServiceBusMessageContext messageContext, MessageTelemetryOptions options)
        {
            ArgumentNullException.ThrowIfNull(messageContext);
            ArgumentNullException.ThrowIfNull(options);

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

    internal static class ILoggerExtensions
    {
        internal static void LogServiceBusRequest(
            this ILogger logger,
            string serviceBusNamespace,
            string entityName,
            string operationName,
            bool isSuccessful,
            TimeSpan duration,
            DateTimeOffset startTime,
            ServiceBusEntityType entityType,
            Dictionary<string, object> context = null)
        {
            if (string.IsNullOrWhiteSpace(operationName))
            {
                operationName = "Azure Service Bus message processing";
            }

            context = context is null ? new Dictionary<string, object>() : new Dictionary<string, object>(context);
            context["ServiceBus-Endpoint"] = serviceBusNamespace;
            context["ServiceBus-EntityName"] = entityName;
            context["ServiceBus-Entity"] = entityType;

            logger.LogWarning("{@Request}", RequestLogEntry.CreateForServiceBus(operationName, isSuccessful, duration, startTime, context));
        }
        /// <summary>
        /// Represents a HTTP request as a log entry.
        /// </summary>
        private sealed class RequestLogEntry
        {
            private RequestLogEntry(
                string method,
                string host,
                string uri,
                string operationName,
                int statusCode,
                RequestSourceSystem sourceSystem,
                string requestTime,
                TimeSpan duration,
                IDictionary<string, object> context)
            {
                RequestMethod = method;
                RequestHost = host;
                RequestUri = uri;
                ResponseStatusCode = statusCode;
                RequestDuration = duration;
                OperationName = operationName;
                SourceSystem = sourceSystem;
                RequestTime = requestTime;
                Context = context;
                Context["TelemetryType"] = "Request";
            }

            /// <summary>
            /// Creates an <see cref="RequestLogEntry"/> instance for Azure Service Bus requests.
            /// </summary>
            /// <param name="operationName">The name of the operation of the request.</param>
            /// <param name="isSuccessful">The indication whether or not the Azure Service Bus request was successfully processed.</param>
            /// <param name="duration">The duration it took to process the Azure Service Bus request.</param>
            /// <param name="startTime">The time when the request was received.</param>
            /// <param name="context">The telemetry context that provides more insights on the Azure Service Bus request.</param>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="duration"/> is a negative time range.</exception>
            public static RequestLogEntry CreateForServiceBus(
                string operationName,
                bool isSuccessful,
                TimeSpan duration,
                DateTimeOffset startTime,
                IDictionary<string, object> context)
            {
                return new RequestLogEntry(
                    method: "<not-applicable>",
                    host: "<not-applicable>",
                    uri: "<not-applicable>",
                    operationName: operationName,
                    statusCode: isSuccessful ? 200 : 500,
                    sourceSystem: RequestSourceSystem.AzureServiceBus,
                    requestTime: startTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff zzz"),
                    duration: duration,
                    context: context);
            }

            /// <summary>
            /// Gets the HTTP method of the request.
            /// </summary>
            public string RequestMethod { get; }

            /// <summary>
            /// Gets the host that was requested.
            /// </summary>
            public string RequestHost { get; }

            /// <summary>
            /// Gets ths URI of the request.
            /// </summary>
            public string RequestUri { get; }

            /// <summary>
            /// Gets the HTTP response status code that was returned by the service.
            /// </summary>
            public int ResponseStatusCode { get; }

            /// <summary>
            /// Gets the duration of the processing of the request.
            /// </summary>
            public TimeSpan RequestDuration { get; }

            /// <summary>
            /// Gets the date when the request occurred.
            /// </summary>
            public string RequestTime { get; }

            /// <summary>
            /// Gets the type of source system from where the request came from.
            /// </summary>
            public RequestSourceSystem SourceSystem { get; set; }

            /// <summary>
            /// Gets the name of the operation of the source system from where the request came from.
            /// </summary>
            public string OperationName { get; }

            /// <summary>
            /// Gets the context that provides more insights on the HTTP request that was tracked.
            /// </summary>
            public IDictionary<string, object> Context { get; }

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>A string that represents the current object.</returns>
            public override string ToString()
            {
                var contextFormatted = $"{{{string.Join("; ", Context.Select(item => $"[{item.Key}, {item.Value}]"))}}}";

                bool isSuccessful = ResponseStatusCode is 200;

                return $"Azure Service Bus from {OperationName} completed in {RequestDuration} at {RequestTime} - (IsSuccessful: {isSuccessful}, Context: {contextFormatted})";
            }
        }

        private enum RequestSourceSystem { AzureServiceBus }
    }
}
