﻿using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.MessageHandling;
using Microsoft.Azure.ServiceBus;

namespace Arcus.Messaging.Tests.Unit.Fixture
{
    /// <summary>
    /// Test version of the <see cref="IFallbackMessageHandler"/> to have a solid implementation.
    /// </summary>
    public class PassThruServiceBusFallbackMessageHandler : IAzureServiceBusFallbackMessageHandler
    {
        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public Task ProcessMessageAsync(
            Message message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
