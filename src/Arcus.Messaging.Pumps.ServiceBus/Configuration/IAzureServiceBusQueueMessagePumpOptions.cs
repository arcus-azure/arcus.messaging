﻿using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// Represents a sub-set of consumer-configurable options available for Azure Service Bus Queue message pumps.
    /// </summary>
    [Obsolete("Will be removed in v3.0 in favor of using the " + nameof(AzureServiceBusMessagePumpOptions) + " directly as there is no difference anymore between queue and topic options")]
    public interface IAzureServiceBusQueueMessagePumpOptions
    {
        /// <summary>
        /// Gets or sets the maximum concurrent calls to process messages.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is less than or equal to zero.</exception>
        int MaxConcurrentCalls { get; set; }

        /// <summary>
        /// Gets or sets the number of messages that will be eagerly requested from
        /// Queues or Subscriptions and queued locally, intended to help maximize throughput
        /// by allowing the processor to receive from a local cache rather than waiting on a service request.
        /// </summary>
        int PrefetchCount { get; set; }

        /// <summary>
        /// Gets or sets the indication whether or not messages should be automatically marked as completed if no exceptions occurred and processing has finished.
        /// </summary>
        /// <remarks>When turned off, clients have to explicitly mark the messages as completed.</remarks>
        bool AutoComplete { get; set; }

        /// <summary>
        /// Gets or sets the flag to indicate whether or not to emit security events during the lifetime of the message pump.
        /// </summary>
        [Obsolete("Will be removed in v3.0 as the direct link to Arcus.Observability will be removed as well")]
        bool EmitSecurityEvents { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        string JobId { get; set; }

        /// <summary>
        /// Gets or sets the timeout when the message pump tries to restart and re-authenticate during key rotation.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than <see cref="TimeSpan.Zero"/>.</exception>
        [Obsolete("Will be removed in v3.0 as the key rotation functionality will be removed as well")]
        TimeSpan KeyRotationTimeout { get; set; }

        /// <summary>
        /// Gets or sets the fallback when the Azure Key Vault notification doesn't get delivered correctly,
        /// how many times should the message pump run into an <see cref="UnauthorizedAccessException"/> before restarting.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        int MaximumUnauthorizedExceptionsBeforeRestart { get; set; }

        /// <summary>
        /// Gets the consumer-configurable options to change the behavior of the message router.
        /// </summary>
        AzureServiceBusMessageRouterOptions Routing { get; }
    }
}