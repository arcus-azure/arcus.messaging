using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Testing;
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
        private const string HealthPortConfigurationName = "ARCUS_HEALTH_PORT";
        private const int TcpPort = 5050;

        private readonly ILogger _logger;

        public TcpHealthListenerTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task TcpHealthProbe_AcceptsTcpConnection_WhenHealthCheckIsHealthy()
        {
            // Arrange
            var service = new TcpHealthService(TcpPort, _logger);
            var options = new WorkerOptions();
            options.Configuration.Add(HealthPortConfigurationName, TcpPort.ToString());
            options.Services.AddTcpHealthProbes(HealthPortConfigurationName, 
                configureHealthChecks: builder => builder.AddCheck("healhty", () => HealthCheckResult.Healthy()),
                configureTcpListenerOptions: opt => opt.RejectTcpConnectionWhenUnhealthy = true);
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
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
            var service = new TcpHealthService(TcpPort, _logger);
            var options = new WorkerOptions();
            options.Configuration.Add(HealthPortConfigurationName, TcpPort.ToString());
            options.Services.AddTcpHealthProbes(HealthPortConfigurationName, 
                configureHealthChecks: builder => builder.AddCheck("unhealhty", () => HealthCheckResult.Unhealthy()),
                configureTcpListenerOptions: opt => opt.RejectTcpConnectionWhenUnhealthy = true,
                configureHealthCheckPublisherOptions: opt => opt.Period = TimeSpan.FromSeconds(3));
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Assert
                await RetryAssert<ThrowsException, SocketException>(
                    () => Assert.ThrowsAnyAsync<SocketException>(() => service.GetHealthReportAsync()));
            }
        }

        [Fact]
        public async Task TcpHealthProbe_RemovesExceptionDetails_WhenHealthCheckIsUnhealthy()
        {
            // Arrange
            var service = new TcpHealthService(TcpPort, _logger);
            var options = new WorkerOptions();
            options.Configuration.Add(HealthPortConfigurationName, TcpPort.ToString());
            options.Services.AddTcpHealthProbes(HealthPortConfigurationName, 
                configureHealthChecks: builder =>
                {
                    builder.AddCheck("unhealhty", () => HealthCheckResult.Unhealthy(
                        description: "Something happened!",
                        data: new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { ["Some"] = "Thing" }),
                        exception: new InvalidOperationException("Something happened!")));
                },
                configureTcpListenerOptions: opt => opt.RejectTcpConnectionWhenUnhealthy = false,
                configureHealthCheckPublisherOptions: opt => opt.Period = TimeSpan.FromSeconds(3));
            
            // Act
            await using (var worker = await Worker.StartNewAsync(options))
            {
                // Assert
                HealthReport afterReport = await RetryAssert<SocketException, HealthReport>(
                    () => service.GetHealthReportAsync());
                Assert.NotNull(afterReport);
                Assert.Equal(HealthStatus.Unhealthy, afterReport.Status);
                Assert.All(afterReport.Entries, item =>
                {
                    Assert.NotNull(item.Value.Description);
                    Assert.NotEmpty(item.Value.Data);
                    Assert.Null(item.Value.Exception);
                });
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
