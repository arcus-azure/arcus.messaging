using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Tests.Unit.ServiceBus.Fixture
{
    public class InMemoryServiceBusSender : ServiceBusSender
    {
        private readonly ConcurrentQueue<ServiceBusMessage> _messages = new ConcurrentQueue<ServiceBusMessage>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryServiceBusSender" /> class.
        /// </summary>
        public InMemoryServiceBusSender()
        {
            FullyQualifiedNamespace = "inmemory.servicebus.windows.net";
            EntityPath = "in-memory";
        }

        /// <summary>
        ///   The fully qualified Service Bus namespace that the producer is associated with.  This is likely
        ///   to be similar to <c>{yournamespace}.servicebus.windows.net</c>.
        /// </summary>
        public override string FullyQualifiedNamespace { get; }

        /// <summary>
        ///   The path of the entity that the sender is connected to, specific to the
        ///   Service Bus namespace that contains it.
        /// </summary>
        public override string EntityPath { get; } 

        public IEnumerable<ServiceBusMessage> Messages => _messages.ToArray();

        /// <summary>
        ///   Sends a set of messages to the associated Service Bus entity using a batched approach.
        ///   If the size of the messages exceed the maximum size of a single batch,
        ///   an exception will be triggered and the send will fail. In order to ensure that the messages
        ///   being sent will fit in a batch, use <see cref="M:Azure.Messaging.ServiceBus.ServiceBusSender.SendMessagesAsync(Azure.Messaging.ServiceBus.ServiceBusMessageBatch,System.Threading.CancellationToken)" /> instead.
        /// </summary>
        /// <param name="messages">The set of messages to send.</param>
        /// <param name="cancellationToken">An optional <see cref="T:System.Threading.CancellationToken" /> instance to signal the request to cancel the operation.</param>
        /// <returns>A task to be resolved on when the operation has completed.</returns>
        /// <exception cref="T:Azure.Messaging.ServiceBus.ServiceBusException">
        ///   The set of messages exceeds the maximum size allowed in a single batch, as determined by the Service Bus service.
        ///   The <see cref="P:Azure.Messaging.ServiceBus.ServiceBusException.Reason" /> will be set to <see cref="F:Azure.Messaging.ServiceBus.ServiceBusFailureReason.MessageSizeExceeded" /> in this case.
        ///   For more information on service limits, see
        ///   <see href="https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas#messaging-quotas" />.
        /// </exception>
        public override Task SendMessagesAsync(IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (ServiceBusMessage message in messages)
            {
                _messages.Enqueue(message);
            }

            return Task.CompletedTask;
        }
    }
}
