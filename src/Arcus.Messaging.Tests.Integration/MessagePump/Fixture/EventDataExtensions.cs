using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Core.Correlation;
using Azure.Messaging.EventHubs;

namespace Arcus.Messaging.Tests.Integration.MessagePump.Fixture
{
    public static class EventDataExtensions
    {
        public static EventData WithDiagnosticId(this EventData message, TraceParent traceParent)
        {
            message.Properties["Diagnostic-Id"] = traceParent.DiagnosticId;
            return message;
        }
    }
}
