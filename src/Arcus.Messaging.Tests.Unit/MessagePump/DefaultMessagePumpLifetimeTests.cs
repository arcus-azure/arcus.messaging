using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Pumps.Abstractions;
using Arcus.Messaging.Tests.Unit.MessagePump.Fixture;
using Arcus.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Arcus.Messaging.Tests.Unit.MessagePump
{
    public class DefaultMessagePumpLifetimeTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMessagePumpLifetimeTests" /> class.
        /// </summary>
        public DefaultMessagePumpLifetimeTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task StopPump_ViaLifetime_Succeeds()
        {
            // Arrange
            string jobId = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddMessagePump(serviceProvider => new TestMessagePump(jobId, Mock.Of<IConfiguration>(), serviceProvider, _logger));
            IServiceProvider provider = services.BuildServiceProvider();
            var hostedService = provider.GetRequiredService<IHostedService>();
            await hostedService.StartAsync(CancellationToken.None);

            var lifetime = new DefaultMessagePumpLifetime(provider);

            // Act
            await lifetime.StopProcessingMessagesAsync(jobId, CancellationToken.None);

            // Assert
            var pump = Assert.IsType<TestMessagePump>(hostedService);
            Assert.False(pump.IsRunning);
        }

        [Fact]
        public async Task StartPump_ViaLifetime_Succeeds()
        {
            // Arrange
            string jobId = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddMessagePump(serviceProvider => new TestMessagePump(jobId, Mock.Of<IConfiguration>(), serviceProvider, _logger));
            IServiceProvider provider = services.BuildServiceProvider();
            var hostedService = provider.GetRequiredService<IHostedService>();
            await hostedService.StopAsync(CancellationToken.None);

            var lifetime = new DefaultMessagePumpLifetime(provider);

            // Act
            await lifetime.StartProcessingMessagesAsync(jobId, CancellationToken.None);

            // Assert
            var pump = Assert.IsType<TestMessagePump>(hostedService);
            Assert.True(pump.IsRunning);
        }

        [Fact]
        public async Task PausePump_ViaLifetime_Succeeds()
        {
            // Arrange
            string jobId = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddMessagePump(serviceProvider => new TestMessagePump(jobId, Mock.Of<IConfiguration>(), serviceProvider, _logger));
            IServiceProvider provider = services.BuildServiceProvider();
            var hostedService = provider.GetRequiredService<IHostedService>();
            await hostedService.StartAsync(CancellationToken.None);

            var lifetime = new DefaultMessagePumpLifetime(provider);
            var duration = TimeSpan.FromSeconds(5);

            // Act
            await lifetime.PauseProcessingMessagesAsync(jobId, duration, CancellationToken.None);

            // Assert
            var pump = Assert.IsType<TestMessagePump>(hostedService);
            Assert.False(pump.IsRunning);

            Policy.Timeout(duration + TimeSpan.FromSeconds(1))
                  .Wrap(Policy.Handle<XunitException>()
                              .WaitAndRetryForever(i => TimeSpan.FromMilliseconds(100)))
                  .Execute(() => Assert.True(pump.IsRunning));
        }
    }
}
