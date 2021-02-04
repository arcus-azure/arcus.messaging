using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.Health
{
    [Trait("Category", "Docker")]
    public class TcpHealthListenerDockerTests : IntegrationTest
    {
        private readonly int _healthTcpPort;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthListenerDockerTests"/> class.
        /// </summary>
        public TcpHealthListenerDockerTests(ITestOutputHelper testOutput) : base(testOutput)
        {
            _healthTcpPort = Configuration.GetValue<int>("Arcus:Health:Port");
        }

        [Fact]
        public async Task TcpHealthListener_ProbeForHealthReport_ResponseHealthy()
        {
            // Arrange
            var service = new TcpHealthService(_healthTcpPort, Logger);
            
            // Act
            HealthReport report = await service.GetHealthReportAsync();
            
            // Assert
            Assert.NotNull(report);
            Assert.Equal(HealthStatus.Healthy, report.Status);
            (string entryName, HealthReportEntry entry) = Assert.Single(report.Entries);
            Assert.Equal("sample", entryName);
            Assert.Equal(HealthStatus.Healthy, entry.Status);
        }
    }
}
