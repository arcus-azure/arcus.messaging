using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Tests.Unit.ServiceBus.Fixture
{
    public class TestProcessMessageEventArgs : ProcessMessageEventArgs
    {
        public TestProcessMessageEventArgs(ServiceBusReceivedMessage message, ServiceBusReceiver receiver, CancellationToken cancellationToken) 
            : base(message, receiver, cancellationToken)
        {
        }
    }
}
