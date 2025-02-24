using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.ServiceBus.Core;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using Microsoft.Extensions.Logging;

#pragma warning disable S1133 // Disable usage of deprecated functionality until v3.0 is released.

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.ServiceBus
{
    /// <summary>
    /// Extensions on the <see cref="ServiceBusSender"/> to more easily send and track correlated Azure Service Bus messages.
    /// </summary>
    public static class ServiceBusSenderExtensions
    {
        /// <summary>
        ///   Sends a message to the associated entity of Service Bus.
        /// </summary>
        /// <param name="sender">The Azure Service Bus sender when sending the <paramref name="messageBody"/></param>
        /// <param name="messageBody">The message contents to send as an Azure Service Bus message.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the to-be-created message with.</param>
        /// <param name="logger">The logger instance to track the Azure Service Bus dependency.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="ServiceBusException">
        ///   The message exceeds the maximum size allowed, as determined by the Service Bus service.
        ///   The <see cref="ServiceBusException.Reason" /> will be set to <see cref="ServiceBusFailureReason.MessageSizeExceeded" /> in this case.
        ///   For more information on service limits, see
        ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas" />.
        /// </exception>
        [Obsolete("Will be removed in v3.0 as this extension on the Azure Service bus sender is only used in the deprecated 'Hierarchical' correlation format")]
        public static async Task SendMessageAsync(
            this ServiceBusSender sender,
            object messageBody,
            CorrelationInfo correlationInfo,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await SendMessageAsync(sender, messageBody, correlationInfo, logger, configureOptions: null, cancellationToken);
        }

        /// <summary>
        ///   Sends a message to the associated entity of Service Bus.
        /// </summary>
        /// <param name="sender">The Azure Service Bus sender when sending the <paramref name="messageBody"/></param>
        /// <param name="messageBody">The message contents to send as an Azure Service Bus message.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the to-be-created message with.</param>
        /// <param name="logger">The logger instance to track the Azure Service Bus dependency.</param>
        /// <param name="configureOptions">The function to configure additional options to the correlated message.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="ServiceBusException">
        ///   The message exceeds the maximum size allowed, as determined by the Service Bus service.
        ///   The <see cref="ServiceBusException.Reason" /> will be set to <see cref="ServiceBusFailureReason.MessageSizeExceeded" /> in this case.
        ///   For more information on service limits, see
        ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas" />.
        /// </exception>
        [Obsolete("Will be removed in v3.0 as this extension on the Azure Service bus sender is only used in the deprecated 'Hierarchical' correlation format")]
        public static async Task SendMessageAsync(
            this ServiceBusSender sender,
            object messageBody,
            CorrelationInfo correlationInfo,
            ILogger logger,
            Action<ServiceBusSenderMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await SendMessagesAsync(sender, new[] { messageBody }, correlationInfo, logger, configureOptions, cancellationToken);
        }

        /// <summary>
        ///   Sends a message to the associated entity of Service Bus.
        /// </summary>
        /// <param name="sender">The Azure Service Bus sender when sending the <paramref name="message"/></param>
        /// <param name="message">The message to send.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="message"/> with.</param>
        /// <param name="logger">The logger instance to track the Azure Service Bus dependency.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="ServiceBusException">
        ///   The message exceeds the maximum size allowed, as determined by the Service Bus service.
        ///   The <see cref="ServiceBusException.Reason" /> will be set to <see cref="ServiceBusFailureReason.MessageSizeExceeded" /> in this case.
        ///   For more information on service limits, see
        ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas" />.
        /// </exception>
        [Obsolete("Will be removed in v3.0 as this extension on the Azure Service bus sender is only used in the deprecated 'Hierarchical' correlation format")]
        public static async Task SendMessageAsync(
            this ServiceBusSender sender,
            ServiceBusMessage message,
            CorrelationInfo correlationInfo,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await SendMessageAsync(sender, message, correlationInfo, logger, configureOptions: null, cancellationToken);
        }

        /// <summary>
        ///   Sends a message to the associated entity of Service Bus.
        /// </summary>
        /// <param name="sender">The Azure Service Bus sender when sending the <paramref name="message"/></param>
        /// <param name="message">The message to send.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="message"/> with.</param>
        /// <param name="logger">The logger instance to track the Azure Service Bus dependency.</param>
        /// <param name="configureOptions">The function to configure additional options to the correlated <paramref name="message"/>.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="ServiceBusException">
        ///   The message exceeds the maximum size allowed, as determined by the Service Bus service.
        ///   The <see cref="ServiceBusException.Reason" /> will be set to <see cref="ServiceBusFailureReason.MessageSizeExceeded" /> in this case.
        ///   For more information on service limits, see
        ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas" />.
        /// </exception>
        [Obsolete("Will be removed in v3.0 as this extension on the Azure Service bus sender is only used in the deprecated 'Hierarchical' correlation format")]
        public static async Task SendMessageAsync(
            this ServiceBusSender sender,
            ServiceBusMessage message,
            CorrelationInfo correlationInfo,
            ILogger logger,
            Action<ServiceBusSenderMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await SendMessagesAsync(sender, new[] { message }, correlationInfo, logger, configureOptions, cancellationToken);
        }

        /// <summary>
        ///   Sends a set of messages to the associated Service Bus entity using a batched approach.
        ///   If the size of the messages exceed the maximum size of a single batch,
        ///   an exception will be triggered and the send will fail. In order to ensure that the messages
        ///   being sent will fit in a batch, use <see cref="ServiceBusSender.SendMessagesAsync(ServiceBusMessageBatch,CancellationToken)" /> instead.
        /// </summary>
        /// <param name="sender">The Azure Service Bus sender when sending the <paramref name="messageBodies"/>.</param>
        /// <param name="messageBodies">The set of message bodies to send as Azure Service Bus messages.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the to-be-created messages with.</param>
        /// <param name="logger">The logger instance to track the Azure Service Bus dependency.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="sender"/>, <paramref name="messageBodies"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="messageBodies"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="ServiceBusException">
        ///   The set of messages exceeds the maximum size allowed in a single batch, as determined by the Service Bus service.
        ///   The <see cref="ServiceBusException.Reason" /> will be set to <see cref="ServiceBusFailureReason.MessageSizeExceeded" /> in this case.
        ///   For more information on service limits, see
        ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas" />.
        /// </exception>
        [Obsolete("Will be removed in v3.0 as this extension on the Azure Service bus sender is only used in the deprecated 'Hierarchical' correlation format")]
        public static async Task SendMessagesAsync(
            this ServiceBusSender sender,
            IEnumerable<object> messageBodies,
            CorrelationInfo correlationInfo,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await SendMessagesAsync(sender, messageBodies, correlationInfo, logger, configureOptions: null, cancellationToken);
        }

        /// <summary>
        ///   Sends a set of messages to the associated Service Bus entity using a batched approach.
        ///   If the size of the messages exceed the maximum size of a single batch,
        ///   an exception will be triggered and the send will fail. In order to ensure that the messages
        ///   being sent will fit in a batch, use <see cref="ServiceBusSender.SendMessagesAsync(ServiceBusMessageBatch,CancellationToken)" /> instead.
        /// </summary>
        /// <param name="sender">The Azure Service Bus sender when sending the <paramref name="messageBodies"/>.</param>
        /// <param name="messageBodies">The set of message bodies to send as Azure Service Bus messages.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the to-be-created messages with.</param>
        /// <param name="logger">The logger instance to track the Azure Service Bus dependency.</param>
        /// <param name="configureOptions">The function to configure additional options to the correlated messages.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="sender"/>, <paramref name="messageBodies"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="messageBodies"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="ServiceBusException">
        ///   The set of messages exceeds the maximum size allowed in a single batch, as determined by the Service Bus service.
        ///   The <see cref="ServiceBusException.Reason" /> will be set to <see cref="ServiceBusFailureReason.MessageSizeExceeded" /> in this case.
        ///   For more information on service limits, see
        ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas" />.
        /// </exception>
        [Obsolete("Will be removed in v3.0 as this extension on the Azure Service bus sender is only used in the deprecated 'Hierarchical' correlation format")]
        public static async Task SendMessagesAsync(
            this ServiceBusSender sender,
            IEnumerable<object> messageBodies,
            CorrelationInfo correlationInfo,
            ILogger logger,
            Action<ServiceBusSenderMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (messageBodies is null)
            {
                throw new ArgumentNullException(nameof(messageBodies));
            }

            ServiceBusMessage[] messages =
                messageBodies.Select(messageBody => ServiceBusMessageBuilder.CreateForBody(messageBody).Build())
                             .ToArray();

            await SendMessagesAsync(sender, messages, correlationInfo, logger, configureOptions, cancellationToken);
        }

        /// <summary>
        ///   Sends a set of messages to the associated Service Bus entity using a batched approach.
        ///   If the size of the messages exceed the maximum size of a single batch,
        ///   an exception will be triggered and the send will fail. In order to ensure that the messages
        ///   being sent will fit in a batch, use <see cref="ServiceBusSender.SendMessagesAsync(ServiceBusMessageBatch,CancellationToken)" /> instead.
        /// </summary>
        /// <param name="sender">The Azure Service Bus sender when sending the <paramref name="messages"/>.</param>
        /// <param name="messages">The set of messages to send.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="messages"/> with.</param>
        /// <param name="logger">The logger instance to track the Azure Service Bus dependency.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="sender"/>, <paramref name="messages"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="messages"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="ServiceBusException">
        ///   The set of messages exceeds the maximum size allowed in a single batch, as determined by the Service Bus service.
        ///   The <see cref="ServiceBusException.Reason" /> will be set to <see cref="ServiceBusFailureReason.MessageSizeExceeded" /> in this case.
        ///   For more information on service limits, see
        ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas" />.
        /// </exception>
        [Obsolete("Will be removed in v3.0 as this extension on the Azure Service bus sender is only used in the deprecated 'Hierarchical' correlation format")]
        public static async Task SendMessagesAsync(
            this ServiceBusSender sender,
            IEnumerable<ServiceBusMessage> messages,
            CorrelationInfo correlationInfo,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await SendMessagesAsync(sender, messages, correlationInfo, logger, configureOptions: null, cancellationToken);
        }

        /// <summary>
        ///   Sends a set of messages to the associated Service Bus entity using a batched approach.
        ///   If the size of the messages exceed the maximum size of a single batch,
        ///   an exception will be triggered and the send will fail. In order to ensure that the messages
        ///   being sent will fit in a batch, use <see cref="ServiceBusSender.SendMessagesAsync(ServiceBusMessageBatch,CancellationToken)" /> instead.
        /// </summary>
        /// <param name="sender">The Azure Service Bus sender when sending the <paramref name="messages"/>.</param>
        /// <param name="messages">The set of messages to send.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="messages"/> with.</param>
        /// <param name="logger">The logger instance to track the Azure Service Bus dependency.</param>
        /// <param name="configureOptions">The function to configure additional options to the correlated <paramref name="messages"/>.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="sender"/>, <paramref name="messages"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="messages"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="ServiceBusException">
        ///   The set of messages exceeds the maximum size allowed in a single batch, as determined by the Service Bus service.
        ///   The <see cref="ServiceBusException.Reason" /> will be set to <see cref="ServiceBusFailureReason.MessageSizeExceeded" /> in this case.
        ///   For more information on service limits, see
        ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas" />.
        /// </exception>
        [Obsolete("Will be removed in v3.0 as this extension on the Azure Service bus sender is only used in the deprecated 'Hierarchical' correlation format")]
        public static async Task SendMessagesAsync(
            this ServiceBusSender sender,
            IEnumerable<ServiceBusMessage> messages,
            CorrelationInfo correlationInfo,
            ILogger logger,
            Action<ServiceBusSenderMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sender is null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            if (correlationInfo is null)
            {
                throw new ArgumentNullException(nameof(correlationInfo));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var options = new ServiceBusSenderMessageCorrelationOptions();
            configureOptions?.Invoke(options);

            string dependencyId = options.GenerateDependencyId();

            messages = messages.ToArray();
            foreach (ServiceBusMessage message in messages)
            {
                message.ApplicationProperties[options.TransactionIdPropertyName] = correlationInfo.TransactionId;
                message.ApplicationProperties[options.UpstreamServicePropertyName] = dependencyId;
            }

            bool isSuccessful = false;
            using (var measurement = DurationMeasurement.Start())
            {
                try
                {
                    await sender.SendMessagesAsync(messages, cancellationToken);
                    isSuccessful = true;
                }
                finally
                {
                    logger.LogServiceBusDependency(sender.FullyQualifiedNamespace, sender.EntityPath, isSuccessful, measurement, dependencyId, options.EntityType, options.TelemetryContext);
                }
            }
        }
    }
}
