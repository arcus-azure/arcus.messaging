using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Workers.ServiceBus;
using Arcus.Testing.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Arcus.Messaging.Tests.Integration.Health
{
    [Trait("Category", "Integration")]
    public class TcpHealthListenerTests
    {
        private readonly ILogger _logger;

        public TcpHealthListenerTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task TcpHealthProbe_AcceptsTcpConnection_WhenHealthCheckIsHealthy()
        {
            // Arrange
            const string healthPortConfigurationName = "ARCUS_HEALTH_PORT";
            const int tcpPort = 5050;
            var service = new TcpHealthService(tcpPort, _logger);
            var options = new WorkerOptions();
            options.Configuration.Add(healthPortConfigurationName, tcpPort.ToString());
            options.Services.AddTcpHealthProbes(healthPortConfigurationName, 
                configureHealthChecks: builder => builder.AddCheck("healhty", () => HealthCheckResult.Healthy()),
                configureTcpListenerOptions: opt => opt.RejectTcpConnectionWhenUnhealthy = true);
            
            // Act
            await using (var worker = Worker.StartNew(options))
            {
                // Assert
                HealthReport afterReport = await RetryAssert<SocketException, HealthReport>(
                    () => service.GetHealthReportAsync());
                Assert.NotNull(afterReport);
                Assert.Equal(HealthStatus.Healthy, afterReport.Status);
            }
        }
        
        [Fact]
        public async Task TcpHealthProbe_RejectsTcpConnection_WhenHealthCheckIsUnhealthy()
        {
            // Arrange
            const string healthPortConfigurationName = "ARCUS_HEALTH_PORT";
            const int tcpPort = 5050;
            var service = new TcpHealthService(tcpPort, _logger);
            var options = new WorkerOptions();
            options.Configuration.Add(healthPortConfigurationName, tcpPort.ToString());
            options.Services.AddTcpHealthProbes(healthPortConfigurationName, 
                configureHealthChecks: builder => builder.AddCheck("unhealhty", () => HealthCheckResult.Unhealthy()),
                configureTcpListenerOptions: opt => opt.RejectTcpConnectionWhenUnhealthy = true,
                configureHealthCheckPublisherOptions: opt => opt.Delay = TimeSpan.Zero);
            
            // Act
            await using (var worker = Worker.StartNew(options))
            {
                // Assert
                await RetryAssert<ThrowsException, SocketException>(
                    () => Assert.ThrowsAnyAsync<SocketException>(() => service.GetHealthReportAsync()));
            }
        }

        private static async Task<TResult> RetryAssert<TException, TResult>(Func<Task<TResult>> assertion) where TException : Exception
        {
            return await Policy.TimeoutAsync(TimeSpan.FromSeconds(30))
                               .WrapAsync(Policy.Handle<TException>()
                                                .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(2)))
                               .ExecuteAsync(assertion);
        }
    }
}
