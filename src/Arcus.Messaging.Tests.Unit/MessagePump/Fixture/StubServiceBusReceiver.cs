using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Tests.Unit.MessagePump.Fixture
{
    /// <summary>
    /// Represents an in-memory <see cref="ServiceBusReceiver"/> that hands out an endless supply of messages,
    /// while recording how the message pump interacts with the receiver.
    /// </summary>
    public class StubServiceBusReceiver : ServiceBusReceiver
    {
        private readonly Func<ServiceBusReceivedMessage> _messageFactory;
        private readonly ConcurrentQueue<string> _abandonedMessageIds = new();
        private readonly ConcurrentQueue<string> _completedMessageIds = new();
        private readonly ConcurrentQueue<string> _deadLetteredMessageIds = new();
        private readonly ConcurrentQueue<int> _requestedBatchSizes = new();
        private readonly ConcurrentDictionary<int, string[]> _messageIdsPerBatch = new();

        private static readonly TimeSpan SimulatedReceiveDelay = TimeSpan.FromMilliseconds(5);

        private int _batchReceiveCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubServiceBusReceiver" /> class.
        /// </summary>
        /// <param name="messageFactory">The factory function that creates a new unique message upon each receive.</param>
        public StubServiceBusReceiver(Func<ServiceBusReceivedMessage> messageFactory)
        {
            ArgumentNullException.ThrowIfNull(messageFactory);
            _messageFactory = messageFactory;
        }

        /// <summary>
        /// Gets or sets the amount of messages that a single batch-receive hands out, regardless of the requested batch size.
        /// </summary>
        public int MessagesPerBatch { get; set; } = 1;

        /// <summary>
        /// Gets the fully-qualified namespace of the simulated Azure Service Bus resource.
        /// </summary>
        /// <remarks>
        ///     Overridden because the base implementation requires an active connection.
        /// </remarks>
        public override string FullyQualifiedNamespace { get; } = "stub.servicebus.windows.net";

        /// <summary>
        /// Gets the entity path of the simulated Azure Service Bus resource.
        /// </summary>
        /// <remarks>
        ///     Overridden because the base implementation requires an active connection.
        /// </remarks>
        public override string EntityPath { get; } = "stub-entity";

        /// <summary>
        /// Gets or sets the hook that runs upon each batch-receive - before any messages are handed out -
        /// with the one-based index of the batch-receive call. Useful to interleave test actions with the pump's receive loop.
        /// </summary>
        public Func<int, CancellationToken, Task> OnReceivingBatchAsync { get; set; } = (_, _) => Task.CompletedTask;

        /// <summary>
        /// Gets the flag that indicates whether the message pump disposed this receiver.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets the message IDs that the message pump abandoned, in order of abandoning.
        /// </summary>
        public IReadOnlyCollection<string> AbandonedMessageIds => _abandonedMessageIds.ToArray();

        /// <summary>
        /// Gets the message IDs that the message pump completed, in order of completion.
        /// </summary>
        public IReadOnlyCollection<string> CompletedMessageIds => _completedMessageIds.ToArray();

        /// <summary>
        /// Gets the message IDs that the message pump dead-lettered, in order of dead-lettering.
        /// </summary>
        public IReadOnlyCollection<string> DeadLetteredMessageIds => _deadLetteredMessageIds.ToArray();

        /// <summary>
        /// Gets the batch sizes that the message pump requested upon each batch-receive.
        /// </summary>
        public IReadOnlyCollection<int> RequestedBatchSizes => _requestedBatchSizes.ToArray();

        /// <summary>
        /// Gets the message IDs that were handed out during the batch-receive with the given one-based <paramref name="batchIndex"/>.
        /// </summary>
        public IReadOnlyCollection<string> MessageIdsOfBatch(int batchIndex)
        {
            return _messageIdsPerBatch.TryGetValue(batchIndex, out string[] messageIds) ? messageIds : Array.Empty<string>();
        }

        public override async Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveMessagesAsync(
            int maxMessages,
            TimeSpan? maxWaitTime = default,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _requestedBatchSizes.Enqueue(maxMessages);

            int batchIndex = Interlocked.Increment(ref _batchReceiveCount);
            await OnReceivingBatchAsync(batchIndex, cancellationToken);

            await Task.Delay(SimulatedReceiveDelay, cancellationToken);

            ServiceBusReceivedMessage[] messages =
                Enumerable.Range(0, Math.Min(maxMessages, MessagesPerBatch))
                          .Select(_ => _messageFactory())
                          .ToArray();

            _messageIdsPerBatch[batchIndex] = messages.Select(msg => msg.MessageId).ToArray();
            return messages;
        }

        public override async Task<ServiceBusReceivedMessage> ReceiveMessageAsync(
            TimeSpan? maxWaitTime = default,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(SimulatedReceiveDelay, cancellationToken);

            return _messageFactory();
        }

        public override Task AbandonMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object> propertiesToModify = default,
            CancellationToken cancellationToken = default)
        {
            _abandonedMessageIds.Enqueue(message.MessageId);
            return Task.CompletedTask;
        }

        public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
        {
            _completedMessageIds.Enqueue(message.MessageId);
            return Task.CompletedTask;
        }

        public override Task DeadLetterMessageAsync(
            ServiceBusReceivedMessage message,
            string deadLetterReason,
            string deadLetterErrorDescription = default,
            CancellationToken cancellationToken = default)
        {
            _deadLetteredMessageIds.Enqueue(message.MessageId);
            return Task.CompletedTask;
        }

        public override Task DeadLetterMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object> propertiesToModify = default,
            string deadLetterReason = default,
            string deadLetterErrorDescription = default,
            CancellationToken cancellationToken = default)
        {
            _deadLetteredMessageIds.Enqueue(message.MessageId);
            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Represents an in-memory <see cref="ServiceBusClient"/> that always hands out the same <see cref="StubServiceBusReceiver"/>.
    /// </summary>
    public class StubServiceBusClient : ServiceBusClient
    {
        private readonly ServiceBusReceiver _receiver;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubServiceBusClient" /> class.
        /// </summary>
        /// <param name="receiver">The receiver that should be used by the message pump.</param>
        public StubServiceBusClient(ServiceBusReceiver receiver)
        {
            ArgumentNullException.ThrowIfNull(receiver);
            _receiver = receiver;
        }

        public override ServiceBusReceiver CreateReceiver(string queueName, ServiceBusReceiverOptions options)
        {
            return _receiver;
        }

        public override ServiceBusReceiver CreateReceiver(string topicName, string subscriptionName, ServiceBusReceiverOptions options)
        {
            return _receiver;
        }
    }
}
