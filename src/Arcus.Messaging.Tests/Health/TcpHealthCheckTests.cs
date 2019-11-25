using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Xunit;

namespace Arcus.Messaging.Tests.Health
{
    public class TcpHealthCheckTests
    {
        [Fact]
        public async Task TcpHealthServer_ProbeForHealthReport_ResponseHealthy()
        {
            // Arrange
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 42063);
            await using NetworkStream networkStream = client.GetStream();
            using var reader = new StreamReader(networkStream);
            
            // Act
            string healthReport = await reader.ReadLineAsync();

            // Assert
            var report = JsonConvert.DeserializeObject<HealthReport>(healthReport);
            Assert.NotNull(report);
            Assert.Equal(HealthStatus.Healthy, report.Status);
        }
    }
}
