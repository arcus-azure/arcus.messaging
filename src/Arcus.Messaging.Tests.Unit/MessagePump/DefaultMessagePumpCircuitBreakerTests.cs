using System;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;
using Arcus.Messaging.Tests.Unit.MessagePump.Fixture;
using Arcus.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcus.Messaging.Tests.Unit.MessagePump
{
    public class DefaultMessagePumpCircuitBreakerTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMessagePumpCircuitBreakerTests" /> class.
        /// </summary>
        public DefaultMessagePumpCircuitBreakerTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task PauseMessagePump_WithoutRegisteredMessagePump_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            IServiceProvider provider = services.BuildServiceProvider();

            DefaultMessagePumpCircuitBreaker breaker = CreateCircuitBreaker(provider);

            // Act / Assert
            var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(
                () => breaker.PauseMessageProcessingAsync("unknown-job-id"));
            Assert.Contains("Cannot find", exception.Message);
        }

        [Fact]
        public async Task PauseMessagePump_WithManyRegisteredMessagePump_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHostedService(p => (Pumps.Abstractions.MessagePump) new TestMessagePump("same-job-id", Mock.Of<IConfiguration>(), p, _logger));
            services.AddHostedService(p => new TestMessagePump("same-job-id", Mock.Of<IConfiguration>(), p, _logger));
            IServiceProvider provider = services.BuildServiceProvider();

            DefaultMessagePumpCircuitBreaker breaker = CreateCircuitBreaker(provider);

            // Act / Assert
            var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(
                () => breaker.PauseMessageProcessingAsync("same-job-id"));
            Assert.Contains("Cannot find", exception.Message);
        }

        private DefaultMessagePumpCircuitBreaker CreateCircuitBreaker(IServiceProvider provider)
        {
            var factory = new LoggerFactory();
            factory.AddProvider(new CustomLoggerProvider(_logger));
            var logger = new Logger<DefaultMessagePumpCircuitBreaker>(factory);

            return new DefaultMessagePumpCircuitBreaker(provider, logger);
        }
    }
}
