using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.ServiceBus.Abstractions;
using Arcus.Messaging.ServiceBus.Abstractions.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;

namespace Arcus.Messaging.Tests.Workers.MessageHandlers
{
    public class CustomerMessageHandler : IAzureServiceBusMessageHandler<Customer>
    {
        /// <summary>
        ///     Process a new message that was received
        /// </summary>
        /// <param name="message">Message that was received</param>
        /// <param name="messageMessageContext">Context providing more information concerning the processing</param>
        /// <param name="correlationInfo">
        ///     Information concerning correlation of telemetry and processes by using a variety of unique
        ///     identifiers
        /// </param>
        /// <param name="cancellationToken">Cancellation token</param>
        public Task ProcessMessageAsync(
            Customer message,
            AzureServiceBusMessageContext messageMessageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
