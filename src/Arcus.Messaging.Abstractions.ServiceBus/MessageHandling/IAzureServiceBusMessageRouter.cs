﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions.MessageHandling;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents an <see cref="IMessageRouter"/> that can route Azure Service Bus <see cref="ServiceBusReceivedMessage"/>s.
    /// </summary>
    public interface IAzureServiceBusMessageRouter : IMessageRouter
    {
        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> or <see cref="IAzureServiceBusFallbackMessageHandler"/>
        /// if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The incoming message that needs to be routed through registered message handlers.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <remarks>
        ///     Note that registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s with specific Azure Service Bus operations (dead-letter, complete...),
        ///     will not be able to call those operations without an <see cref="ServiceBusReceiver"/>.
        ///     Use the <see cref="RouteMessageAsync(ServiceBusReceiver,ServiceBusReceivedMessage,AzureServiceBusMessageContext,MessageCorrelationInfo,CancellationToken)"/> instead.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        Task RouteMessageAsync(
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);

        /// <summary>
        /// Handle a new <paramref name="message"/> that was received by routing them through registered <see cref="IAzureServiceBusMessageHandler{TMessage}"/>s
        /// and optionally through an registered <see cref="IFallbackMessageHandler"/> or <see cref="IAzureServiceBusFallbackMessageHandler"/>
        /// if none of the message handlers were able to process the <paramref name="message"/>.
        /// </summary>
        /// <param name="messageReceiver">
        ///     The receiver that can call operations (dead letter, complete...) on an Azure Service Bus <see cref="ServiceBusReceivedMessage"/>;
        ///     used within <see cref="AzureServiceBusMessageHandler{TMessage}"/>s or <see cref="AzureServiceBusFallbackMessageHandler"/>s.
        /// </param>
        /// <param name="message">The incoming message that needs to be routed through registered message handlers.</param>
        /// <param name="messageContext">The context in which the <paramref name="message"/> should be processed.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the message processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="messageReceiver"/>, <paramref name="message"/>, <paramref name="messageContext"/>, or <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when no message handlers or none matching message handlers are found to process the message.</exception>
        Task RouteMessageAsync(
            ServiceBusReceiver messageReceiver,
            ServiceBusReceivedMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);
    }
}