using System;
using Arcus.Messaging.Abstractions.MessageHandling;

namespace Arcus.Messaging.Pumps.ServiceBus.Configuration
{
    /// <summary>
    /// Represents a sub-set of consumer-configurable options available for Azure Service Bus Queue message pumps.
    /// </summary>
    public interface IAzureServiceBusQueueMessagePumpOptions
    {
        /// <summary>
        /// Gets or sets the maximum concurrent calls to process messages.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is less than or equal to zero.</exception>
        int? MaxConcurrentCalls { get; set; }
        
        /// <summary>
        /// Gets or sets the indication whether or not messages should be automatically marked as completed if no exceptions occurred and processing has finished.
        /// </summary>
        /// <remarks>When turned off, clients have to explicitly mark the messages as completed.</remarks>
        bool AutoComplete { get; set; }

        /// <summary>
        /// Gets or sets the flag to indicate whether or not to emit security events during the lifetime of the message pump.
        /// </summary>
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
        TimeSpan KeyRotationTimeout { get; set; }

        /// <summary>
        /// Gets or sets the fallback when the Azure Key Vault notification doesn't get delivered correctly,
        /// how many times should the message pump run into an <see cref="UnauthorizedAccessException"/> before restarting.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is less than zero.</exception>
        int MaximumUnauthorizedExceptionsBeforeRestart { get; set; }

        /// <summary>
        /// Gets the options to control the correlation information upon the receiving of Azure Service Bus messages in the <see cref="AzureServiceBusMessagePump"/>.
        /// </summary>
        AzureServiceBusCorrelationOptions Correlation { get; }
        
        /// <summary>
        /// Gets the consumer-configurable options to change the deserialization behavior.
        /// </summary>
         MessageDeserializationOptions Deserialization { get; }
    }
}