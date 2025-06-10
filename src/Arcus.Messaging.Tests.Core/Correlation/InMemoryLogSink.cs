using System.Collections.Generic;
using System.Collections.ObjectModel;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.Testing
{
    public class InMemoryLogSink : ILogEventSink
    {
        private readonly Collection<LogEvent> _events = new();

        public IReadOnlyCollection<LogEvent> CurrentLogEmits => _events;

        public void Emit(LogEvent logEvent)
        {
            _events.Add(logEvent);
        }
    }
}
