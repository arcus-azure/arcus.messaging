using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.MessageHandling;
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Pumps.ServiceBus.Resiliency;
using Arcus.Messaging.Tests.Core.Messages.v1;
using GuardNet;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Arcus.Messaging.Tests.Workers.ServiceBus.MessageHandlers
{
    /// <summary>
    /// Represents a message handler that interacts with the <see cref="IMessagePumpCircuitBreaker"/>
    /// in providing simulated failures which starts and stops the related message pump.
    /// </summary>
    public class TestCircuitBreakerAzureServiceBusMessageHandler : CircuitBreakerServiceBusMessageHandler<Shipment>
    {
        private readonly string[] _targetMessageIds;
        private readonly Action<MessagePumpCircuitBreakerOptions> _configureOptions;

        private static readonly ICollection<(Shipment message, DateTimeOffset arrival)> MessageArrivals = new Collection<(Shipment, DateTimeOffset)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCircuitBreakerAzureServiceBusMessageHandler" /> class.
        /// </summary>
        public TestCircuitBreakerAzureServiceBusMessageHandler(
            string[] targetMessageIds,
            Action<MessagePumpCircuitBreakerOptions> configureOptions,
            IMessagePumpCircuitBreaker circuitBreaker,
            ILogger<TestCircuitBreakerAzureServiceBusMessageHandler> logger) : base(circuitBreaker, logger)
        {
            Guard.NotNull(targetMessageIds, nameof(targetMessageIds));
            Guard.NotAny(targetMessageIds, nameof(targetMessageIds));
            Guard.NotNull(circuitBreaker, nameof(circuitBreaker));

            _targetMessageIds = targetMessageIds;
            _configureOptions = configureOptions;
        }

        /// <summary>
        /// Process a new message that was received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        /// <param name="messageContext">The context providing more information concerning the processing.</param>
        /// <param name="correlationInfo">The information concerning correlation of telemetry and processes by using a variety of unique identifiers.</param>
        /// <param name="options">The additional options to manipulate the possible circuit breakage of the message pump for which a message is processed.</param>
        /// <param name="cancellationToken">The token to cancel the processing.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="message"/>, <paramref name="messageContext"/>, or the <paramref name="correlationInfo"/> is <c>null</c>.
        /// </exception>
        protected override Task ProcessMessageAsync(
            Shipment message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            MessagePumpCircuitBreakerOptions options,
            CancellationToken cancellationToken)
        {
            _configureOptions(options);

            if (!_targetMessageIds.Contains(message.Id))
            {
                return Task.CompletedTask;
            }

            MessageArrivals.Add((message, DateTimeOffset.UtcNow));
            if (MessageArrivals.Count < 3)
            {
                Logger.LogError("Sabotage unavailable dependency system");
                throw new InvalidOperationException("Simulated sabotage of unavailable dependency system");
            }

            Logger.LogInformation("Recovered from simulated unavailable dependency system");
            return Task.CompletedTask;
        }

        public DateTimeOffset[] GetMessageArrivals()
        {
            var arrivals = MessageArrivals.Select(a => a.arrival).ToArray();
            Assert.Equal(3, arrivals.Length);

            return arrivals;
        }
    }
}
