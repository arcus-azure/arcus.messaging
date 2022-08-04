using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.EventHubs.Core;
using Arcus.Observability.Telemetry.Core;
using GuardNet;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.EventHubs.Producer
{
    /// <summary>
    /// Extensions on the <see cref="EventHubProducerClient"/> to send correlated event messages.
    /// </summary>
    public static class EventHubProducerClientExtensions
    {
        /// <summary>
        ///     Sends a set of events to the associated Event Hub as a single operation, including adding message correlation and dependency tracking.
        ///     To avoid the overhead associated with measuring and validating the size in the client, validation will be delegated to the Event Hubs service and is deferred until the operation is invoked.
        ///     The call will fail if the size of the specified set of events exceeds the maximum allowable size of a single batch.
        /// </summary>
        /// <param name="client">The client that will produce the <paramref name="eventBatch"/> to Azure EventHubs.</param>
        /// <param name="eventBatch">The set of event bodies to send.</param>
        /// <param name="logger">The logger instance to track the Azure EventHubs dependency.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="eventBatch"/> with.</param>
        /// <returns>
        ///     A task to be resolved on when the operation has completed; if no exception is thrown when awaited,
        ///     the Event Hubs service has acknowledged receipt and assumed responsibility for delivery of the set of events to its partition.
        /// </returns>
        /// <remarks>
        ///     When published, the result is atomic; either all events that belong to the set were successful or all have failed.
        ///     Partial success is not possible.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/>, <paramref name="eventBatch"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventBatch"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="EventHubsException">
        ///     Thrown when the set of events exceeds the maximum size allowed in a single batch, as determined by the Event Hubs service.
        ///     The <see cref="P:Azure.Messaging.EventHubs.EventHubsException.Reason" /> will be set to
        ///   <see cref="EventHubsException.FailureReason.MessageSizeExceeded" /> in this case.
        /// </exception>
        /// <exception cref="SerializationException">
        ///     Thrown when one of the events in the <paramref name="eventBatch" /> has a member in the <see cref="EventData.Properties" /> collection
        ///     that is an unsupported type for serialization.  See the <see cref="EventData.Properties" /> remarks for details.
        /// </exception>
        public static async Task SendAsync(
            this EventHubProducerClient client,
            IEnumerable<object> eventBatch,
            MessageCorrelationInfo correlationInfo,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(client, nameof(client), "Requires an Azure EventHubs producer client while sending a correlated message");
            Guard.NotNull(eventBatch, nameof(eventBatch), "Requires a series of Azure EventHubs messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure EventHubs dependency while sending the correlated messages");
            Guard.NotAny(eventBatch, nameof(eventBatch), "Requires at least a single message to send to Azure EventHubs");
            Guard.For(() => eventBatch.Any(message => message is null), new ArgumentException("Requires non-null items in Azure EventHubs message sequence", nameof(eventBatch)));

            await SendAsync(client, eventBatch, correlationInfo, logger, sendEventOptions: null, configureOptions: null, cancellationToken);
        }

        /// <summary>
        ///     Sends a set of events to the associated Event Hub as a single operation, including adding message correlation and dependency tracking.
        ///     To avoid the overhead associated with measuring and validating the size in the client, validation will be delegated to the Event Hubs service and is deferred until the operation is invoked.
        ///     The call will fail if the size of the specified set of events exceeds the maximum allowable size of a single batch.
        /// </summary>
        /// <param name="client">The client that will produce the <paramref name="eventBatch"/> to Azure EventHubs.</param>
        /// <param name="eventBatch">The set of event bodies to send.</param>
        /// <param name="logger">The logger instance to track the Azure EventHubs dependency.</param>
        /// <param name="configureOptions">The function to configure additional options to the correlated <paramref name="eventBatch"/>.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="eventBatch"/> with.</param>
        /// <returns>
        ///     A task to be resolved on when the operation has completed; if no exception is thrown when awaited,
        ///     the Event Hubs service has acknowledged receipt and assumed responsibility for delivery of the set of events to its partition.
        /// </returns>
        /// <remarks>
        ///     When published, the result is atomic; either all events that belong to the set were successful or all have failed.
        ///     Partial success is not possible.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/>, <paramref name="eventBatch"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventBatch"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="EventHubsException">
        ///     Thrown when the set of events exceeds the maximum size allowed in a single batch, as determined by the Event Hubs service.
        ///     The <see cref="EventHubsException.Reason" /> will be set to
        ///   <see cref="EventHubsException.FailureReason.MessageSizeExceeded" /> in this case.
        /// </exception>
        /// <exception cref="SerializationException">
        ///     Thrown when one of the events in the <paramref name="eventBatch" /> has a member in the <see cref="EventData.Properties" /> collection
        ///     that is an unsupported type for serialization.  See the <see cref="EventData.Properties" /> remarks for details.
        /// </exception>
        public static async Task SendAsync(
            this EventHubProducerClient client,
            IEnumerable<object> eventBatch,
            MessageCorrelationInfo correlationInfo,
            ILogger logger,
            Action<EventHubProducerClientMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(client, nameof(client), "Requires an Azure EventHubs producer client while sending a correlated message");
            Guard.NotNull(eventBatch, nameof(eventBatch), "Requires a series of Azure EventHubs messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure EventHubs dependency while sending the correlated messages");
            Guard.NotAny(eventBatch, nameof(eventBatch), "Requires at least a single message to send to Azure EventHubs");
            Guard.For(() => eventBatch.Any(message => message is null), new ArgumentException("Requires non-null items in Azure EventHubs message sequence", nameof(eventBatch)));

            await SendAsync(client, eventBatch, correlationInfo, logger, sendEventOptions: null, configureOptions, cancellationToken);
        }

        /// <summary>
        ///     Sends a set of events to the associated Event Hub as a single operation, including adding message correlation and dependency tracking.
        ///     To avoid the overhead associated with measuring and validating the size in the client, validation will be delegated to the Event Hubs service and is deferred until the operation is invoked.
        ///     The call will fail if the size of the specified set of events exceeds the maximum allowable size of a single batch.
        /// </summary>
        /// <param name="client">The client that will produce the <paramref name="eventBatch"/> to Azure EventHubs.</param>
        /// <param name="eventBatch">The set of event bodies to send.</param>
        /// <param name="logger">The logger instance to track the Azure EventHubs dependency.</param>
        /// <param name="sendEventOptions">The set of options to consider when sending this batch.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="eventBatch"/> with.</param>
        /// <returns>
        ///     A task to be resolved on when the operation has completed; if no exception is thrown when awaited,
        ///     the Event Hubs service has acknowledged receipt and assumed responsibility for delivery of the set of events to its partition.
        /// </returns>
        /// <remarks>
        ///     When published, the result is atomic; either all events that belong to the set were successful or all have failed.
        ///     Partial success is not possible.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/>, <paramref name="eventBatch"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventBatch"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="InvalidOperationException">Thrown when both a partition identifier and partition key have been specified in the <paramref name="sendEventOptions" />.</exception>
        /// <exception cref="EventHubsException">
        ///     Thrown when the set of events exceeds the maximum size allowed in a single batch, as determined by the Event Hubs service.
        ///     The <see cref="EventHubsException.Reason" /> will be set to
        ///   <see cref="EventHubsException.FailureReason.MessageSizeExceeded" /> in this case.
        /// </exception>
        /// <exception cref="SerializationException">
        ///     Thrown when one of the events in the <paramref name="eventBatch" /> has a member in the <see cref="EventData.Properties" /> collection
        ///     that is an unsupported type for serialization.  See the <see cref="EventData.Properties" /> remarks for details.
        /// </exception>
        public static async Task SendAsync(
            this EventHubProducerClient client,
            IEnumerable<object> eventBatch,
            MessageCorrelationInfo correlationInfo,
            ILogger logger,
            SendEventOptions sendEventOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(client, nameof(client), "Requires an Azure EventHubs producer client while sending a correlated message");
            Guard.NotNull(eventBatch, nameof(eventBatch), "Requires a series of Azure EventHubs messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure EventHubs dependency while sending the correlated messages");
            Guard.NotAny(eventBatch, nameof(eventBatch), "Requires at least a single message to send to Azure EventHubs");
            Guard.For(() => eventBatch.Any(message => message is null), new ArgumentException("Requires non-null items in Azure EventHubs message sequence", nameof(eventBatch)));

            await SendAsync(client, eventBatch, correlationInfo, logger, sendEventOptions, configureOptions: null, cancellationToken);
        }

        /// <summary>
        ///     Sends a set of events to the associated Event Hub as a single operation, including adding message correlation and dependency tracking.
        ///     To avoid the overhead associated with measuring and validating the size in the client, validation will be delegated to the Event Hubs service and is deferred until the operation is invoked.
        ///     The call will fail if the size of the specified set of events exceeds the maximum allowable size of a single batch.
        /// </summary>
        /// <param name="client">The client that will produce the <paramref name="eventBatch"/> to Azure EventHubs.</param>
        /// <param name="eventBatch">The set of event bodies to send.</param>
        /// <param name="logger">The logger instance to track the Azure EventHubs dependency.</param>
        /// <param name="sendEventOptions">The set of options to consider when sending this batch.</param>
        /// <param name="configureOptions">The function to configure additional options to the correlated <paramref name="eventBatch"/>.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="eventBatch"/> with.</param>
        /// <returns>
        ///     A task to be resolved on when the operation has completed; if no exception is thrown when awaited,
        ///     the Event Hubs service has acknowledged receipt and assumed responsibility for delivery of the set of events to its partition.
        /// </returns>
        /// <remarks>
        ///     When published, the result is atomic; either all events that belong to the set were successful or all have failed.
        ///     Partial success is not possible.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/>, <paramref name="eventBatch"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventBatch"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="InvalidOperationException">Thrown when both a partition identifier and partition key have been specified in the <paramref name="sendEventOptions" />.</exception>
        /// <exception cref="EventHubsException">
        ///     Thrown when the set of events exceeds the maximum size allowed in a single batch, as determined by the Event Hubs service.
        ///     The <see cref="EventHubsException.Reason" /> will be set to
        ///   <see cref="EventHubsException.FailureReason.MessageSizeExceeded" /> in this case.
        /// </exception>
        /// <exception cref="SerializationException">
        ///     Thrown when one of the events in the <paramref name="eventBatch" /> has a member in the <see cref="EventData.Properties" /> collection
        ///     that is an unsupported type for serialization.  See the <see cref="EventData.Properties" /> remarks for details.
        /// </exception>
        public static async Task SendAsync(
            this EventHubProducerClient client,
            IEnumerable<object> eventBatch,
            MessageCorrelationInfo correlationInfo,
            ILogger logger,
            SendEventOptions sendEventOptions,
            Action<EventHubProducerClientMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            EventData[] eventMessages = eventBatch.Select(ev => EventDataBuilder.CreateForBody(ev).Build()).ToArray();
            await SendAsync(client, eventMessages, correlationInfo, logger, sendEventOptions, configureOptions, cancellationToken);
        }

        /// <summary>
        ///     Sends a set of events to the associated Event Hub as a single operation, including adding message correlation and dependency tracking.
        ///     To avoid the overhead associated with measuring and validating the size in the client, validation will be delegated to the Event Hubs service and is deferred until the operation is invoked.
        ///     The call will fail if the size of the specified set of events exceeds the maximum allowable size of a single batch.
        /// </summary>
        /// <param name="client">The client that will produce the <paramref name="eventBatch"/> to Azure EventHubs.</param>
        /// <param name="eventBatch">The set of event data to send.</param>
        /// <param name="logger">The logger instance to track the Azure EventHubs dependency.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="eventBatch"/> with.</param>
        /// <returns>
        ///     A task to be resolved on when the operation has completed; if no exception is thrown when awaited,
        ///     the Event Hubs service has acknowledged receipt and assumed responsibility for delivery of the set of events to its partition.
        /// </returns>
        /// <remarks>
        ///     When published, the result is atomic; either all events that belong to the set were successful or all have failed.
        ///     Partial success is not possible.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/>, <paramref name="eventBatch"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventBatch"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="EventHubsException">
        ///     Thrown when the set of events exceeds the maximum size allowed in a single batch, as determined by the Event Hubs service.
        ///     The <see cref="P:Azure.Messaging.EventHubs.EventHubsException.Reason" /> will be set to
        ///   <see cref="EventHubsException.FailureReason.MessageSizeExceeded" /> in this case.
        /// </exception>
        /// <exception cref="SerializationException">
        ///     Thrown when one of the events in the <paramref name="eventBatch" /> has a member in the <see cref="EventData.Properties" /> collection
        ///     that is an unsupported type for serialization.  See the <see cref="EventData.Properties" /> remarks for details.
        /// </exception>
        public static async Task SendAsync(
            this EventHubProducerClient client,
            IEnumerable<EventData> eventBatch,
            MessageCorrelationInfo correlationInfo,
            ILogger logger,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(client, nameof(client), "Requires an Azure EventHubs producer client while sending a correlated message");
            Guard.NotNull(eventBatch, nameof(eventBatch), "Requires a series of Azure EventHubs messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure EventHubs dependency while sending the correlated messages");
            Guard.NotAny(eventBatch, nameof(eventBatch), "Requires at least a single message to send to Azure EventHubs");
            Guard.For(() => eventBatch.Any(message => message is null), new ArgumentException("Requires non-null items in Azure EventHubs message sequence", nameof(eventBatch)));

            await SendAsync(client, eventBatch, correlationInfo, logger, sendEventOptions: null, configureOptions: null, cancellationToken);
        }

        /// <summary>
        ///     Sends a set of events to the associated Event Hub as a single operation, including adding message correlation and dependency tracking.
        ///     To avoid the overhead associated with measuring and validating the size in the client, validation will be delegated to the Event Hubs service and is deferred until the operation is invoked.
        ///     The call will fail if the size of the specified set of events exceeds the maximum allowable size of a single batch.
        /// </summary>
        /// <param name="client">The client that will produce the <paramref name="eventBatch"/> to Azure EventHubs.</param>
        /// <param name="eventBatch">The set of event data to send.</param>
        /// <param name="logger">The logger instance to track the Azure EventHubs dependency.</param>
        /// <param name="configureOptions">The function to configure additional options to the correlated <paramref name="eventBatch"/>.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="eventBatch"/> with.</param>
        /// <returns>
        ///     A task to be resolved on when the operation has completed; if no exception is thrown when awaited,
        ///     the Event Hubs service has acknowledged receipt and assumed responsibility for delivery of the set of events to its partition.
        /// </returns>
        /// <remarks>
        ///     When published, the result is atomic; either all events that belong to the set were successful or all have failed.
        ///     Partial success is not possible.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/>, <paramref name="eventBatch"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventBatch"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="EventHubsException">
        ///     Thrown when the set of events exceeds the maximum size allowed in a single batch, as determined by the Event Hubs service.
        ///     The <see cref="EventHubsException.Reason" /> will be set to
        ///   <see cref="EventHubsException.FailureReason.MessageSizeExceeded" /> in this case.
        /// </exception>
        /// <exception cref="SerializationException">
        ///     Thrown when one of the events in the <paramref name="eventBatch" /> has a member in the <see cref="EventData.Properties" /> collection
        ///     that is an unsupported type for serialization.  See the <see cref="EventData.Properties" /> remarks for details.
        /// </exception>
        public static async Task SendAsync(
            this EventHubProducerClient client,
            IEnumerable<EventData> eventBatch,
            MessageCorrelationInfo correlationInfo,
            ILogger logger,
            Action<EventHubProducerClientMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(client, nameof(client), "Requires an Azure EventHubs producer client while sending a correlated message");
            Guard.NotNull(eventBatch, nameof(eventBatch), "Requires a series of Azure EventHubs messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure EventHubs dependency while sending the correlated messages");
            Guard.NotAny(eventBatch, nameof(eventBatch), "Requires at least a single message to send to Azure EventHubs");
            Guard.For(() => eventBatch.Any(message => message is null), new ArgumentException("Requires non-null items in Azure EventHubs message sequence", nameof(eventBatch)));

            await SendAsync(client, eventBatch, correlationInfo, logger, sendEventOptions: null, configureOptions, cancellationToken);
        }

        /// <summary>
        ///     Sends a set of events to the associated Event Hub as a single operation, including adding message correlation and dependency tracking.
        ///     To avoid the overhead associated with measuring and validating the size in the client, validation will be delegated to the Event Hubs service and is deferred until the operation is invoked.
        ///     The call will fail if the size of the specified set of events exceeds the maximum allowable size of a single batch.
        /// </summary>
        /// <param name="client">The client that will produce the <paramref name="eventBatch"/> to Azure EventHubs.</param>
        /// <param name="eventBatch">The set of event data to send.</param>
        /// <param name="logger">The logger instance to track the Azure EventHubs dependency.</param>
        /// <param name="sendEventOptions">The set of options to consider when sending this batch.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="eventBatch"/> with.</param>
        /// <returns>
        ///     A task to be resolved on when the operation has completed; if no exception is thrown when awaited,
        ///     the Event Hubs service has acknowledged receipt and assumed responsibility for delivery of the set of events to its partition.
        /// </returns>
        /// <remarks>
        ///     When published, the result is atomic; either all events that belong to the set were successful or all have failed.
        ///     Partial success is not possible.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/>, <paramref name="eventBatch"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventBatch"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="InvalidOperationException">Thrown when both a partition identifier and partition key have been specified in the <paramref name="sendEventOptions" />.</exception>
        /// <exception cref="EventHubsException">
        ///     Thrown when the set of events exceeds the maximum size allowed in a single batch, as determined by the Event Hubs service.
        ///     The <see cref="EventHubsException.Reason" /> will be set to
        ///   <see cref="EventHubsException.FailureReason.MessageSizeExceeded" /> in this case.
        /// </exception>
        /// <exception cref="SerializationException">
        ///     Thrown when one of the events in the <paramref name="eventBatch" /> has a member in the <see cref="EventData.Properties" /> collection
        ///     that is an unsupported type for serialization.  See the <see cref="EventData.Properties" /> remarks for details.
        /// </exception>
        public static async Task SendAsync(
            this EventHubProducerClient client,
            IEnumerable<EventData> eventBatch,
            MessageCorrelationInfo correlationInfo,
            ILogger logger,
            SendEventOptions sendEventOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(client, nameof(client), "Requires an Azure EventHubs producer client while sending a correlated message");
            Guard.NotNull(eventBatch, nameof(eventBatch), "Requires a series of Azure EventHubs messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure EventHubs dependency while sending the correlated messages");
            Guard.NotAny(eventBatch, nameof(eventBatch), "Requires at least a single message to send to Azure EventHubs");
            Guard.For(() => eventBatch.Any(message => message is null), new ArgumentException("Requires non-null items in Azure EventHubs message sequence", nameof(eventBatch)));

            await SendAsync(client, eventBatch, correlationInfo, logger, sendEventOptions, configureOptions: null, cancellationToken);
        }

        /// <summary>
        ///     Sends a set of events to the associated Event Hub as a single operation, including adding message correlation and dependency tracking.
        ///     To avoid the overhead associated with measuring and validating the size in the client, validation will be delegated to the Event Hubs service and is deferred until the operation is invoked.
        ///     The call will fail if the size of the specified set of events exceeds the maximum allowable size of a single batch.
        /// </summary>
        /// <param name="client">The client that will produce the <paramref name="eventBatch"/> to Azure EventHubs.</param>
        /// <param name="eventBatch">The set of event data to send.</param>
        /// <param name="logger">The logger instance to track the Azure EventHubs dependency.</param>
        /// <param name="sendEventOptions">The set of options to consider when sending this batch.</param>
        /// <param name="configureOptions">The function to configure additional options to the correlated <paramref name="eventBatch"/>.</param>
        /// <param name="cancellationToken">The optional <see cref="CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <param name="correlationInfo">The message correlation instance to enrich the <paramref name="eventBatch"/> with.</param>
        /// <returns>
        ///     A task to be resolved on when the operation has completed; if no exception is thrown when awaited,
        ///     the Event Hubs service has acknowledged receipt and assumed responsibility for delivery of the set of events to its partition.
        /// </returns>
        /// <remarks>
        ///     When published, the result is atomic; either all events that belong to the set were successful or all have failed.
        ///     Partial success is not possible.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/>, <paramref name="eventBatch"/>, or <paramref name="logger"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="eventBatch"/> doesn't contain any elements or has any <c>null</c> elements.</exception>
        /// <exception cref="InvalidOperationException">Thrown when both a partition identifier and partition key have been specified in the <paramref name="sendEventOptions" />.</exception>
        /// <exception cref="EventHubsException">
        ///     Thrown when the set of events exceeds the maximum size allowed in a single batch, as determined by the Event Hubs service.
        ///     The <see cref="EventHubsException.Reason" /> will be set to
        ///   <see cref="EventHubsException.FailureReason.MessageSizeExceeded" /> in this case.
        /// </exception>
        /// <exception cref="SerializationException">
        ///     Thrown when one of the events in the <paramref name="eventBatch" /> has a member in the <see cref="EventData.Properties" /> collection
        ///     that is an unsupported type for serialization.  See the <see cref="EventData.Properties" /> remarks for details.
        /// </exception>
        public static async Task SendAsync(
            this EventHubProducerClient client, 
            IEnumerable<EventData> eventBatch,
            MessageCorrelationInfo correlationInfo,
            ILogger logger,
            SendEventOptions sendEventOptions,
            Action<EventHubProducerClientMessageCorrelationOptions> configureOptions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Guard.NotNull(client, nameof(client), "Requires an Azure EventHubs producer client while sending a correlated message");
            Guard.NotNull(eventBatch, nameof(eventBatch), "Requires a series of Azure EventHubs messages to send as correlated messages");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a message correlation instance to include the transaction ID in the send out messages");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the Azure EventHubs dependency while sending the correlated messages");
            Guard.NotAny(eventBatch, nameof(eventBatch), "Requires at least a single message to send to Azure EventHubs");
            Guard.For(() => eventBatch.Any(message => message is null), new ArgumentException("Requires non-null items in Azure EventHubs message sequence", nameof(eventBatch)));

            var options = new EventHubProducerClientMessageCorrelationOptions();
            configureOptions?.Invoke(options);

            string dependencyId = options.GenerateDependencyId();

            eventBatch = eventBatch.ToArray();
            foreach (EventData eventData in eventBatch)
            {
                eventData.Properties[options.TransactionIdPropertyName] = correlationInfo.TransactionId;
                eventData.Properties[options.UpstreamServicePropertyName] = dependencyId;
            }

            bool isSuccessful = false;
            using (var measurement = DurationMeasurement.Start())
            {
                try
                {
                    await client.SendAsync(eventBatch, sendEventOptions, cancellationToken);
                    isSuccessful = true;
                }
                finally
                {
                    logger.LogEventHubsDependency(client.FullyQualifiedNamespace, client.EventHubName, isSuccessful, measurement, dependencyId);
                }
            }
        }
    }
}
