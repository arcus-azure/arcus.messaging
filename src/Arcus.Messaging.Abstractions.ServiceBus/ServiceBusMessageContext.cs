using System;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Arcus.Messaging
{
    /// <summary>
    /// Represents a message handler that can handle messages from an Azure Service Bus entity.
    /// </summary>
    /// <typeparam name="TMessage">The type of message that the handler can process.</typeparam>
    public interface IServiceBusMessageHandler<in TMessage> : IMessageHandler<TMessage, ServiceBusMessageContext>
    {
    }

    /// <summary>
    /// Represents the contextual information concerning an Azure Service Bus message.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public class ServiceBusMessageContext : AzureServiceBusMessageContext
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private ServiceBusMessageContext(
            string jobId,
            ServiceBusEntityType entityType,
            ServiceBusReceiver receiver,
            ServiceBusReceivedMessage message)
            : base(jobId, receiver.FullyQualifiedNamespace, entityType, receiver.EntityPath, new MessageSettleViaReceiver(receiver, message), message)
        {
        }

        private ServiceBusMessageContext(
            string jobId,
            ServiceBusEntityType entityType,
            ProcessSessionMessageEventArgs eventArgs)
            : base(jobId, eventArgs.FullyQualifiedNamespace, entityType, eventArgs.EntityPath, new MessageSettleViaSessionEventArgs(eventArgs), eventArgs.Message)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="AzureServiceBusMessageContext"/> based on the current Azure Service bus situation.
        /// </summary>
        /// <param name="jobId">The unique ID to identity the Azure Service bus message pump that is responsible for pumping messages from the <paramref name="receiver"/>.</param>
        /// <param name="entityType">The type of Azure Service bus entity that the <paramref name="receiver"/> receives from.</param>
        /// <param name="receiver">The Azure Service bus receiver that is responsible for receiving the <paramref name="message"/>.</param>
        /// <param name="message">The Azure Service bus message that is currently being processed.</param>
        /// <exception cref="ArgumentNullException">Thrown when one of the parameters is <c>null</c>.</exception>
        public static new ServiceBusMessageContext Create(
            string jobId,
            ServiceBusEntityType entityType,
            ServiceBusReceiver receiver,
            ServiceBusReceivedMessage message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
            ArgumentNullException.ThrowIfNull(receiver);
            ArgumentNullException.ThrowIfNull(message);

            return new ServiceBusMessageContext(jobId, entityType, receiver, message);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="AzureServiceBusMessageContext"/> based on the current Azure Service bus situation.
        /// </summary>
        /// <param name="jobId">The unique ID to identity the Azure Service bus message pump that is responsible for pumping messages from the <paramref name="eventArgs"/>.</param>
        /// <param name="entityType">The type of Azure Service bus entity that the <paramref name="eventArgs"/> receives from.</param>
        /// <param name="eventArgs">The Azure Service bus event arguments upon receiving the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when one of the parameters is <c>null</c>.</exception>
        public static ServiceBusMessageContext Create(
            string jobId,
            ServiceBusEntityType entityType,
            ProcessSessionMessageEventArgs eventArgs)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
            ArgumentNullException.ThrowIfNull(eventArgs);

            return new ServiceBusMessageContext(jobId, entityType, eventArgs);
        }
    }
}
