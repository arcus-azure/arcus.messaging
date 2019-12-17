using System;
using GuardNet;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Core.Logging
{
    /// <summary>
    /// <see cref="ILogger"/> representation of a xUnit <see cref="ITestOutputHelper"/> logger.
    /// </summary>
    public class XunitTestLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="XunitTestLogger"/> class.
        /// </summary>
        /// <param name="testOutput">The xUnit test output logger.</param>
        public XunitTestLogger(ITestOutputHelper testOutput)
        {
            Guard.NotNull(testOutput, nameof(testOutput));

            _testOutput = testOutput;
        }

        /// <summary>Writes a log entry.</summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <c>string</c> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            _testOutput.WriteLine($"{DateTimeOffset.UtcNow:s} {logLevel} > {message}");
        }

        /// <summary>
        /// Checks if the given <paramref name="logLevel" /> is enabled.
        /// </summary>
        /// <param name="logLevel">level to be checked.</param>
        /// <returns><c>true</c> if enabled.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>Begins a logical operation scope.</summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
