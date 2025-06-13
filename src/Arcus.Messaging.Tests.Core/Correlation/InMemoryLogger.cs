using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace Arcus.Testing
{
    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; }
    }

    public class InMemoryLogger : ILogger
    {
        private readonly Collection<string> _messages = new();
        private readonly Collection<LogEntry> _entries = new();

        public IReadOnlyCollection<string> Messages => _messages;
        public IReadOnlyCollection<LogEntry> Entries => _entries;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);
            _entries.Add(new LogEntry { Message = message, Level = logLevel });
            _messages.Add(message);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }
    }
}
