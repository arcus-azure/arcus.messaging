using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Tests.Core.Messages.v1;
using GuardNet;

namespace Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers
{
    /// <summary>
    /// Represents a message handler that interacts with the <see cref="IMessagePumpCircuitBreaker"/>
    /// in providing simulated failures which starts and stops the related message pump.
    /// </summary>
    public class CircuitBreakerAzureServiceBusMessageHandler : IAzureServiceBusMessageHandler<Shipment>
    {
        private readonly string[] _targetMessageIds;
        private readonly Action<MessagePumpCircuitBreakerOptions> _configureOptions;
        private readonly IMessagePumpCircuitBreaker _circuitBreaker;
        
        private static readonly ICollection<(Shipment, DateTimeOffset)> MessageArrivals = new Collection<(Shipment, DateTimeOffset)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CircuitBreakerAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public CircuitBreakerAzureServiceBusMessageHandler(
            string[] targetMessageIds,
            Action<MessagePumpCircuitBreakerOptions> configureOptions,
            IMessagePumpCircuitBreaker circuitBreaker)
        {
            Guard.NotNull(targetMessageIds, nameof(targetMessageIds));
            Guard.NotAny(targetMessageIds, nameof(targetMessageIds));
            Guard.NotNull(circuitBreaker, nameof(circuitBreaker));

            _targetMessageIds = targetMessageIds;
            _configureOptions = configureOptions;
            _circuitBreaker = circuitBreaker;
        }

        /// <summary>
        /// Process a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or the <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        public async Task ProcessMessageAsync(
            Shipment message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            if (!_targetMessageIds.Contains(message.Id))
            {
                return;
            }

            MessageArrivals.Add((message, DateTimeOffset.UtcNow));
            if (MessageArrivals.Count < _targetMessageIds.Length)
            {
                await _circuitBreaker.PauseMessageProcessingAsync(messageContext.JobId, _configureOptions);
            }
        }

        public DateTimeOffset[] GetMessageArrivals()
        {
            return _targetMessageIds.Select(id => MessageArrivals.FirstOrDefault(a => a.Item1.Id == id).Item2).Where(a => a != default)
                                    .ToArray();
        }
    }
}
