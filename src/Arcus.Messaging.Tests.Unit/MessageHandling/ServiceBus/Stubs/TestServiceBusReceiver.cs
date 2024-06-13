using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Tests.Unit.MessageHandling.ServiceBus.Stubs
{
    public class TestServiceBusReceiver : ServiceBusReceiver
    {
        public override string FullyQualifiedNamespace { get; } = "arcus.testing.azure.net";

        public bool HasCompletedMessage { get; private set; }

        public override Task CompleteMessageAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = new CancellationToken())
        {
            HasCompletedMessage = true;
            return Task.CompletedTask;
        }
    }
}
