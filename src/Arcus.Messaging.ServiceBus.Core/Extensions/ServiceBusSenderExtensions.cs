using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.ServiceBus.Core;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using GuardNet;
using Microsoft.Extensions.Logging;

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
        public static async Task SendMessageAsync(
            this ServiceBusSender sender,
            object messageBody,
            CorrelationInfo correlationInfo,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(sender, nameof(sender), "Requires an Azure Service Bus sender to while sending a correlated message");
            Guard.NotNull(messageBody, nameof(messageBody), "Requires a series of Azure Service Bus messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure Service Bus dependency while sending the correlated messages");

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
        public static async Task SendMessageAsync(
            this ServiceBusSender sender,
            object messageBody,
            CorrelationInfo correlationInfo,
            ILogger logger,
            Action<ServiceBusSenderMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(sender, nameof(sender), "Requires an Azure Service Bus sender to while sending a correlated message");
            Guard.NotNull(messageBody, nameof(messageBody), "Requires a series of Azure Service Bus messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure Service Bus dependency while sending the correlated messages");

            await SendMessagesAsync(sender, new [] { messageBody }, correlationInfo, logger, configureOptions, cancellationToken);
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
        public static async Task SendMessagesAsync(
            this ServiceBusSender sender,
            IEnumerable<object> messageBodies,
            CorrelationInfo correlationInfo,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(sender, nameof(sender), "Requires an Azure Service Bus sender to while sending a correlated message");
            Guard.NotNull(messageBodies, nameof(messageBodies), "Requires a series of Azure Service Bus messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure Service Bus dependency while sending the correlated messages");
            Guard.NotAny(messageBodies, nameof(messageBodies), "Requires at least a single message to send to Azure Service Bus");
            Guard.For(() => messageBodies.Any(message => message is null), new ArgumentException("Requires non-null items in Azure Service Bus message sequence", nameof(messageBodies)));

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
        public static async Task SendMessagesAsync(
            this ServiceBusSender sender,
            IEnumerable<object> messageBodies,
            CorrelationInfo correlationInfo,
            ILogger logger,
            Action<ServiceBusSenderMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(sender, nameof(sender), "Requires an Azure Service Bus sender to while sending a correlated message");
            Guard.NotNull(messageBodies, nameof(messageBodies), "Requires a series of Azure Service Bus messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure Service Bus dependency while sending the correlated messages");
            Guard.NotAny(messageBodies, nameof(messageBodies), "Requires at least a single message to send to Azure Service Bus");
            Guard.For(() => messageBodies.Any(message => message is null), new ArgumentException("Requires non-null items in Azure Service Bus message sequence", nameof(messageBodies)));

            ServiceBusMessage[] messages =
                messageBodies.Select(messageBody => ServiceBusMessageBuilder.CreateForBody(messageBody).Build())
                             .ToArray();

            await SendMessagesAsync(sender, messages, correlationInfo, logger, configureOptions, cancellationToken);
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
        public static async Task SendMessageAsync(
            this ServiceBusSender sender,
            ServiceBusMessage message,
            CorrelationInfo correlationInfo,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(sender, nameof(sender), "Requires an Azure Service Bus sender to while sending a correlated message");
            Guard.NotNull(message, nameof(message), "Requires a Azure Service Bus message to send as a correlated message");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure Service Bus dependency while sending the correlated messages");

            await SendMessageAsync(sender, message , correlationInfo, logger, configureOptions: null, cancellationToken);
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
        public static async Task SendMessageAsync(
            this ServiceBusSender sender,
            ServiceBusMessage message,
            CorrelationInfo correlationInfo,
            ILogger logger,
            Action<ServiceBusSenderMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(sender, nameof(sender), "Requires an Azure Service Bus sender to while sending a correlated message");
            Guard.NotNull(message, nameof(message), "Requires a Azure Service Bus message to send as a correlated message");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure Service Bus dependency while sending the correlated messages");

            await SendMessagesAsync(sender, new[] { message }, correlationInfo, logger, configureOptions, cancellationToken);
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
        public static async Task SendMessagesAsync(
            this ServiceBusSender sender,
            IEnumerable<ServiceBusMessage> messages,
            CorrelationInfo correlationInfo,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(sender, nameof(sender), "Requires an Azure Service Bus sender to while sending a correlated message");
            Guard.NotNull(messages, nameof(messages), "Requires a series of Azure Service Bus messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure Service Bus dependency while sending the correlated messages");
            Guard.NotAny(messages, nameof(messages), "Requires at least a single message to send to Azure Service Bus");
            Guard.For(() => messages.Any(message => message is null), new ArgumentException("Requires non-null items in Azure Service Bus message sequence", nameof(messages)));

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
        public static async Task SendMessagesAsync(
            this ServiceBusSender sender, 
            IEnumerable<ServiceBusMessage> messages, 
            CorrelationInfo correlationInfo,
            ILogger logger,
            Action<ServiceBusSenderMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(sender, nameof(sender), "Requires an Azure Service Bus sender to send a correlated message");
            Guard.NotNull(messages, nameof(messages), "Requires a series of Azure Service Bus messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure Service Bus dependency while sending the correlated messages");
            Guard.NotAny(messages, nameof(messages), "Requires at least a single message to send to Azure Service Bus");
            Guard.For(() => messages.Any(message => message is null), new ArgumentException("Requires non-null items in Azure Service Bus message sequence", nameof(messages)));

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
