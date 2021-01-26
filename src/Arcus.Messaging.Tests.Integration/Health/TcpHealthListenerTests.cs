using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Arcus.Messaging.Tests.Integration.Fixture;
using Arcus.Messaging.Tests.Workers.ServiceBus;
using Arcus.Testing.Logging;
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
        public async Task TcpHealthListenerWithRejectionActivated_RejectsTcpConnection_WhenHealthCheckIsUnhealthy()
        {
            // Arrange
            var config = TestConfig.Create();
            var service = new TcpHealthService(WorkerProject.HealthPort, _logger);
            
            using (var project = await WorkerProject.StartNewWithAsync<TcpConnectionRejectionProgram>(config, _logger))
            {
                HealthReport afterReport = await RetryAssert<SocketException, HealthReport>(
                    () => service.GetHealthReportAsync());
                Assert.NotNull(afterReport);
                Assert.Equal(HealthStatus.Healthy, afterReport.Status);
                
                // Act / Assert
                await RetryAssert<ThrowsException, SocketException>(
                    () => Assert.ThrowsAnyAsync<SocketException>(() => service.GetHealthReportAsync()));
            }
        }

        private static async Task<TResult> RetryAssert<TException, TResult>(Func<Task<TResult>> assertion) where TException : Exception
        {
            return await Policy.TimeoutAsync(TimeSpan.FromSeconds(30))
                               .WrapAsync(Policy.Handle<TException>()
                                                .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(1)))
                               .ExecuteAsync(assertion);
        }
    }
}
