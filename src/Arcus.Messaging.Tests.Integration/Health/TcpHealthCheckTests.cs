using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Messaging.Tests.Integration.Health
{
    [Trait("Category", "Integration")]
    public class TcpHealthCheckTests : IntegrationTest
    {
        private readonly int _healthTcpPort;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpHealthCheckTests"/> class.
        /// </summary>
        public TcpHealthCheckTests(ITestOutputHelper testOutput) : base(testOutput)
        {
            _healthTcpPort = Configuration.GetValue<int>("Arcus:Health:Port");
        }

        [Fact]
        public async Task TcpHealthServer_ProbeForHealthReport_ResponseHealthy()
        {
            // Arrange
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), _healthTcpPort);
                using (NetworkStream clientStream = client.GetStream())
                using (var reader = new StreamReader(clientStream))
                {
                    // Act
                    string healthReport = await reader.ReadLineAsync();

                    // Assert
                    var report = JsonConvert.DeserializeObject<HealthReport>(healthReport);
                    Assert.NotNull(report);
                    Assert.Equal(HealthStatus.Healthy, report.Status);
                }
            }
        }
    }
}
