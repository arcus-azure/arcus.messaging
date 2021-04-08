using System;
using GuardNet;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.Messaging.Abstractions.ServiceBus.MessageHandling
{
    /// <summary>
    /// Represents the default template when handling Azure Service Bus messages and controlling how the message is being handled by Azure Service Bus.
    /// </summary>
    public abstract class AzureServiceBusMessageHandlerTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusMessageHandlerTemplate"/> class.
        /// </summary>
        /// <param name="logger">The logger instance to write diagnostic messages during the message handling.</param>
        protected AzureServiceBusMessageHandlerTemplate(ILogger logger)
        {
            Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Gets the Azure Service Bus message lock token of the  message that is currently being handled.
        /// </summary>
        internal string LockToken { get; private set; }

        /// <summary>
        /// Gets the specific message receiver to control Azure Service Bus specific operations.
        /// </summary>
        internal MessageReceiver MessageReceiver { get; private set; }

        /// <summary>
        /// Gets the logger to write diagnostic messages during the handling of the message.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Sets the message receiver to the message handler template.
        /// </summary>
        /// <param name="messageReceiver">The message receiver to handle Azure Service Bus message-specific operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="messageReceiver"/> is <c>null</c>.</exception>
        internal void SetMessageReceiver(MessageReceiver messageReceiver)
        {
            Guard.NotNull(messageReceiver, nameof(messageReceiver), "Requires a message receiver to run Azure Service Bus message-specific operations");
            
            Logger.LogTrace("Setting message receiver on message handler");
            MessageReceiver = messageReceiver;
        }

        /// <summary>
        /// Sets the message lock token on the message handler template.
        /// </summary>
        /// <param name="lockToken">The message lock token to handle Azure Service Bus message-specific operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="lockToken"/> is blank.</exception>
        internal void SetLockToken(string lockToken)
        {
            Guard.NotNullOrWhitespace(lockToken, nameof(lockToken), "Requires a non-blank lock token to run Azure Service Bus message-specific operations");

            Logger.LogTrace("Setting message on the message lock token");
            LockToken = lockToken;
        }
    }
}