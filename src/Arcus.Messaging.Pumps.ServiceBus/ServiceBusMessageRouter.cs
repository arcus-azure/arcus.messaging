using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Abstractions.Telemetry;
using Arcus.Observability.Telemetry.Core;
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
            using DurationMeasurement measurement = DurationMeasurement.Start();
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
#pragma warning disable CS0618 // Type or member is obsolete
                Logger.LogServiceBusRequest(messageContext.FullyQualifiedNamespace, messageContext.EntityPath, Options.Telemetry.OperationName, isSuccessful, measurement, messageContext.EntityType);
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
                catch (ServiceBusException exception) when (
                    exception.Message.Contains("lock")
                    && exception.Message.Contains("expired")
                    && exception.Message.Contains("already")
                    && exception.Message.Contains("removed"))
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
}
