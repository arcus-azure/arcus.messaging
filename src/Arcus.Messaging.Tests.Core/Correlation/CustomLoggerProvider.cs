using System;
using Microsoft.Extensions.Logging;

namespace Arcus.Testing
{
    public class CustomLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomLoggerProvider"/> class.
        /// </summary>
        public CustomLoggerProvider(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }
    }
}
