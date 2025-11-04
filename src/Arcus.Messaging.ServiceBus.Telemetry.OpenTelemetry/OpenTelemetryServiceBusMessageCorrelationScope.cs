using System;
using System.Diagnostics;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus.Telemetry;
using Arcus.Messaging.Abstractions.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.ServiceBus.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Represents the OpenTelemetry implementation of the <see cref="IServiceBusMessageCorrelationScope"/>
    /// to track the correlation information of a received Azure Service Bus message within a message pump.
    /// </summary>
    internal class OpenTelemetryServiceBusMessageCorrelationScope : IServiceBusMessageCorrelationScope
    {
        private readonly ActivitySource _activitySource;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTelemetryServiceBusMessageCorrelationScope"/> class.
        /// </summary>
        internal OpenTelemetryServiceBusMessageCorrelationScope(ActivitySource activitySource, ILogger<OpenTelemetryServiceBusMessageCorrelationScope> logger)
        {
            ArgumentNullException.ThrowIfNull(activitySource);
            _activitySource = activitySource;
            _logger = logger ?? NullLogger<OpenTelemetryServiceBusMessageCorrelationScope>.Instance;
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

            _logger.LogTrace("Start Azure Service Bus request '{OperationName}' operation...", options.OperationName);
            (string transactionId, string operationParentId) = messageContext.Properties.GetTraceParent();

            ActivityContext context = new(
                ActivityTraceId.CreateFromString(transactionId),
                ActivitySpanId.CreateFromString(operationParentId),
                ActivityTraceFlags.None);

            Activity activity = _activitySource.CreateActivity(
                name: options.OperationName,
                kind: ActivityKind.Consumer,
                context);

            activity?.Start();
            if (activity is null)
            {
                return new UnlinkedMessageOperationResult(transactionId, operationParentId);
            }

            activity.SetTag("az.namespace", "Microsoft.ServiceBus");
            activity.SetTag("messaging.system", "servicebus");
            activity.SetTag("messaging.operation.type", "receive");
            activity.SetTag("messaging.destination.name", messageContext.EntityPath);
            activity.SetTag("messaging.message.id", messageContext.MessageId);
            activity.SetTag("network.protocol.name", "amqp");

            activity.SetTag("ServiceBus-Endpoint", messageContext.FullyQualifiedNamespace);
            activity.SetTag("ServiceBus-Entity", messageContext.EntityPath);
            activity.SetTag("ServiceBus-EntityType", messageContext.EntityType.ToString());

            return new OpenTelemetryMessageOperationResult(activity, _logger);
        }

        private sealed class OpenTelemetryMessageOperationResult : MessageOperationResult
        {
            private readonly Activity _activity;
            private readonly ILogger _logger;

            internal OpenTelemetryMessageOperationResult(Activity activity, ILogger logger)
                : base(new MessageCorrelationInfo(activity.TraceId.ToString(), activity.SpanId.ToString(), activity.ParentSpanId.ToString()))
            {
                _activity = activity;
                _logger = logger;
            }

            protected override void StopOperation(bool isSuccessful, DateTimeOffset startTime, TimeSpan duration)
            {
                _logger.LogTrace("Stop Azure Service Bus request '{OperationName}' operation (isSuccessful={IsSuccessful})", _activity.OperationName, isSuccessful);

                _activity.SetStatus(isSuccessful ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                _activity.SetTag("messaging.operation.name", isSuccessful ? "ack" : "nack");

                _activity.SetEndTime(_activity.StartTimeUtc.Add(duration));
                _activity.Dispose();
            }
        }

        private sealed class UnlinkedMessageOperationResult : MessageOperationResult
        {
            internal UnlinkedMessageOperationResult(string transactionId, string operationParentId)
                : base(new MessageCorrelationInfo(Guid.NewGuid().ToString(), transactionId, operationParentId))
            {
            }

            protected override void StopOperation(bool isSuccessful, DateTimeOffset startTime, TimeSpan duration)
            {
            }
        }
    }
}
