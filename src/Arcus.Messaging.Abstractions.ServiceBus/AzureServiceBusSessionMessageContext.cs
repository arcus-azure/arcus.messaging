using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Arcus.Messaging.Abstractions.ServiceBus
{
    /// <summary>
    /// Represents the contextual information concerning an Azure Service Bus message that is bound to a ServiceBus session.
    /// </summary>
    public class AzureServiceBusSessionMessageContext : AzureServiceBusMessageContext
    {
        private readonly ProcessSessionMessageEventArgs _processSessionMessageEventArgs;

        private AzureServiceBusSessionMessageContext(string jobId,
            ServiceBusEntityType entityType,
            ProcessSessionMessageEventArgs messageArgs) : base(jobId, entityType, null, messageArgs.Message)
        {
            _processSessionMessageEventArgs = messageArgs;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="AzureServiceBusSessionMessageContext"/> based on the current Azure Service bus situation.
        /// </summary>
        /// <param name="jobId">The unique ID to identity the Azure Service bus message pump that is responsible for pumping messages.</param>
        /// <param name="entityType">The type of Azure Service bus entity that has been received from.</param>
        /// <param name="messageArgs">The <see cref="ProcessSessionMessageEventArgs"/> instances that was received by.</param>
        /// <returns>An <see cref="AzureServiceBusSessionMessageContext"/> instance</returns>
        public static AzureServiceBusSessionMessageContext Create(
            string jobId,
            ServiceBusEntityType entityType,
            ProcessSessionMessageEventArgs messageArgs)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Requires a non-blank job ID to identity an Azure Service bus message pump", nameof(jobId));
            }

            if (messageArgs is null)
            {
                throw new ArgumentNullException(nameof(messageArgs));
            }

            return new AzureServiceBusSessionMessageContext(jobId, entityType, messageArgs);
        }

        /// <inheritdoc/>
        public override async Task CompleteMessageAsync(CancellationToken cancellationToken)
        {
            await _processSessionMessageEventArgs.CompleteMessageAsync(_processSessionMessageEventArgs.Message, cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task AbandonMessageAsync(IDictionary<string, object> newMessageProperties, CancellationToken cancellationToken)
        {
            await _processSessionMessageEventArgs.AbandonMessageAsync(_processSessionMessageEventArgs.Message, newMessageProperties, cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task DeadLetterMessageAsync(string deadLetterReason, string deadLetterErrorDescription,
            CancellationToken cancellationToken)
        {
            await _processSessionMessageEventArgs.DeadLetterMessageAsync(_processSessionMessageEventArgs.Message,
                deadLetterReason, deadLetterErrorDescription, cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task DeadLetterMessageAsync(string deadLetterReason, string deadLetterErrorDescription,
            IDictionary<string, object> newMessageProperties, CancellationToken cancellationToken)
        {
            await _processSessionMessageEventArgs.DeadLetterMessageAsync(_processSessionMessageEventArgs.Message,
                newMessageProperties.ToDictionary(),
                deadLetterReason, deadLetterErrorDescription, cancellationToken);
        }
    }
}
