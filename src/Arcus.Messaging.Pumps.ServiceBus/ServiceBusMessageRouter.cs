using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Abstractions.Telemetry;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using static Arcus.Messaging.Abstractions.MessageHandling.MessageProcessingError;

namespace Arcus.Messaging.Pumps.ServiceBus
{
    /// <summary>
    /// Represents an <see cref="IAzureServiceBusMessageRouter"/> that can route Azure Service Bus <see cref="ServiceBusReceivedMessage"/>s.
    /// </summary>
    internal class ServiceBusMessageRouter : MessageRouter
    {
        private readonly AzureServiceBusMessageRouterOptions _serviceBusOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusMessageRouter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider instance to retrieve all the <see cref="IAzureServiceBusMessageHandler{TMessage}"/> instances.</param>
        /// <param name="options">The consumer-configurable options to change the behavior of the router.</param>
        /// <param name="logger">The logger instance to write diagnostic trace messages during the routing of the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        internal ServiceBusMessageRouter(IServiceProvider serviceProvider, AzureServiceBusMessageRouterOptions options, ILogger<ServiceBusMessageRouter> logger)
            : base(serviceProvider, options, logger)
        {
            _serviceBusOptions = options;
        }

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s.
        /// </summary>
        /// <param name="message">The incoming message that needs to be routed through registered message handlers.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        internal async Task<MessageProcessingResult> RouteMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            var watch = Stopwatch.StartNew();

            using IServiceScope serviceScope = ServiceProvider.CreateScope();
#pragma warning disable CS0618 // Type or member is obsolete: will be refactored when moving towards v3.0.
            using IDisposable _ = LogContext.Push(new MessageCorrelationInfoEnricher(correlationInfo, Options.CorrelationEnricher));
#pragma warning restore CS0618 // Type or member is obsolete

            bool isSuccessful = false;
            try
            {
                MessageProcessingResult result = await TryRoutingMessageViaRegisteredMessageHandlersAsync(serviceScope.ServiceProvider, message, messageContext, correlationInfo, cancellationToken);
                isSuccessful = result.IsSuccessful;

                return result;
            }
            finally
            {
                watch.Stop();
#pragma warning disable CS0618 // Type or member is obsolete
                Logger.LogServiceBusRequest(messageContext.FullyQualifiedNamespace, messageContext.EntityPath, Options.Telemetry.OperationName, isSuccessful, watch.Elapsed, startTime, messageContext.EntityType);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        private async Task<MessageProcessingResult> TryRoutingMessageViaRegisteredMessageHandlersAsync(
            IServiceProvider serviceProvider,
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                MessageHandler[] messageHandlers = GetRegisteredMessageHandlers(serviceProvider).ToArray();
                if (messageHandlers.Length <= 0)
                {
                    await DeadLetterMessageNoHandlerRegisteredAsync(messageContext);
                    return MessageProcessingResult.Failure(message.MessageId, CannotFindMatchedHandler, "Failed to process message in the message pump as no message handler is registered in the dependency container");
                }

                string messageBody = LoadMessageBody(message, messageContext);
                bool hasGoneThroughMessageHandler = false;

                foreach (MessageHandler messageHandler in messageHandlers)
                {
                    MessageResult result = await DeserializeMessageForHandlerAsync(messageBody, messageContext, messageHandler);
                    if (result.IsSuccess)
                    {
                        bool isProcessed = await messageHandler.ProcessMessageAsync(result.DeserializedMessage, messageContext, correlationInfo, cancellationToken);

                        hasGoneThroughMessageHandler = true;
                        if (isProcessed)
                        {
                            await PotentiallyAutoCompleteMessageAsync(messageContext);
                            return MessageProcessingResult.Success(message.MessageId);
                        }
                    }
                }

                if (hasGoneThroughMessageHandler)
                {
                    await AbandonMessageMatchedHandlerFailedAsync(messageContext);
                    return MessageProcessingResult.Failure(message.MessageId, MatchedHandlerFailed, "Failed to process Azure Service Bus message in pump as the matched handler did not successfully processed the message");
                }

                await DeadLetterMessageNoHandlerMatchedAsync(messageContext);
                return MessageProcessingResult.Failure(message.MessageId, CannotFindMatchedHandler, "Failed to process Azure Service Bus message in pump as no message handler was matched against the message");
            }
            catch (Exception exception)
            {
                Logger.LogCritical(exception, "Unable to process message with ID '{MessageId}'", message.MessageId);

                await messageContext.AbandonMessageAsync(new Dictionary<string, object>(), CancellationToken.None);
                return MessageProcessingResult.Failure(message.MessageId, ProcessingInterrupted, "Failed to process message in pump as there was an unexpected critical problem during processing, please see the logs for more information", exception);
            }
        }

        private static string LoadMessageBody(ServiceBusReceivedMessage message, AzureServiceBusMessageContext context)
        {
            Encoding encoding = DetermineEncoding();
            string messageBody = encoding.GetString(message.Body.ToArray());

            return messageBody;

            Encoding DetermineEncoding()
            {
                Encoding fallbackEncoding = Encoding.UTF8;

                if (context.Properties.TryGetValue(PropertyNames.Encoding, out object encodingNameObj)
                    && encodingNameObj is string encodingName
                    && !string.IsNullOrWhiteSpace(encodingName))
                {
                    EncodingInfo foundEncoding =
                        Encoding.GetEncodings()
                                .FirstOrDefault(e => e.Name.Equals(encodingName, StringComparison.OrdinalIgnoreCase));

                    return foundEncoding?.GetEncoding() ?? fallbackEncoding;
                }

                return fallbackEncoding;
            }
        }

        private async Task PotentiallyAutoCompleteMessageAsync(AzureServiceBusMessageContext messageContext)
        {
            if (_serviceBusOptions.AutoComplete)
            {
                try
                {
                    Logger.LogTrace("Auto-complete message '{MessageId}' (if needed) after processing in Azure Service Bus in message pump '{JobId}'", messageContext.MessageId, messageContext.JobId);
                    await messageContext.CompleteMessageAsync(CancellationToken.None);
                }
                catch (ServiceBusException exception) when (exception.Reason is ServiceBusFailureReason.MessageLockLost)
                {
                    Logger.LogTrace(exception, "Message '{MessageId}' on Azure Service Bus in message pump '{JobId}' does not need to be auto-completed, because it was already settled", messageContext.MessageId, messageContext.JobId);
                }
            }
        }

        private async Task DeadLetterMessageNoHandlerRegisteredAsync(AzureServiceBusMessageContext messageContext)
        {
            Logger.LogError("Failed to process Azure Service Bus message '{MessageId}' in pump '{JobId}' as no message handlers were registered in the application services, dead-lettering message!", messageContext.MessageId, messageContext.JobId);
            await messageContext.DeadLetterMessageAsync(CannotFindMatchedHandler.ToString(), "No message handlers were registered in the application services", CancellationToken.None);
        }

        private async Task DeadLetterMessageNoHandlerMatchedAsync(AzureServiceBusMessageContext messageContext)
        {
            Logger.LogError("Failed to process Azure Service Bus message '{MessageId}' in pump '{JobId}' as no registered message handler was matched against the message, dead-lettering message!", messageContext.MessageId, messageContext.JobId);
            await messageContext.DeadLetterMessageAsync(CannotFindMatchedHandler.ToString(), "No registered message handler was matched against the message", CancellationToken.None);
        }

        private async Task AbandonMessageMatchedHandlerFailedAsync(AzureServiceBusMessageContext messageContext)
        {
            Logger.LogDebug("Failed to process Azure Service Bus message '{MessageId}' in pump '{JobId}' as the matched message handler did not successfully process the message, abandoning message!", messageContext.MessageId, messageContext.JobId);
            await messageContext.AbandonMessageAsync(new Dictionary<string, object>(), CancellationToken.None);
        }
    }

    internal static class ILoggerExtensions
    {
        public static void LogServiceBusRequest(
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
            context["ServiceBus-EntityType"] = entityType;

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
                var contextFormatted = $"{{{String.Join("; ", Context.Select(item => $"[{item.Key}, {item.Value}]"))}}}";

                string source = DetermineSource();
                bool isSuccessful = ResponseStatusCode is 200;

                return $"{source} from {OperationName} completed in {RequestDuration} at {RequestTime} - (IsSuccessful: {isSuccessful}, Context: {contextFormatted})";
            }

            private string DetermineSource()
            {
                switch (SourceSystem)
                {
                    case RequestSourceSystem.AzureServiceBus: return "Azure Service Bus";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(SourceSystem), "Cannot determine request source as it represents something outside the bounds of the enumeration");
                }
            }
        }
        private enum RequestSourceSystem { AzureServiceBus }
    }
}
