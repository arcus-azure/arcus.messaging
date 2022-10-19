using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Correlation;
using Azure.Messaging.ServiceBus;

namespace Arcus.Messaging.Tests.Integration.MessagePump.Fixture
{
    public static class ServiceBusMessageExtensions
    {
        public static ServiceBusMessage WithDiagnosticId(this ServiceBusMessage message, TraceParent traceParent)
        {
            message.ApplicationProperties["Diagnostic-Id"] = traceParent.DiagnosticId;
            return message;
        }
    }
}
