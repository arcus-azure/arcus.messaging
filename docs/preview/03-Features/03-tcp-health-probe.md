# TCP Health probe
The `Arcus.Messaging.Health` library provides a TCP health probe endpoint registration  that allows a runtime to periodically check the liveness/readiness of the host based on the [.NET Core health checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks).

This helps in contexts where the health of the application needs to be checked over non-HTTP communication.

## Installation
This features requires to install our NuGet package:

```powershell
PM > Install-Package Arcus.Messaging.Health
```

## Usage
To include the TCP endpoint, add the `.AddTcpHealthProbe(<tcp-port>)` to the [Health checks registration](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks#register-health-check-services):

```csharp
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder();

        builder.Services.AddHealthChecks()
                        .AddCheck("healthy", () => HealthCheckResult.Healthy())
                        .AddTcpHealthProbe(tcpPort: 5050);

        var host = builder.Build();
        host.Run();
    }
}
```

### Customization
The following options are available to manipulate the behavior of the TCP health probe:

```csharp
using Arcus.Messaging.Health;

services.AddHealthChecks()
        .AddTcpHealthProbe(..., options =>
        {
            // Let the TCP probe know that when the health report is 'unhealthy',
            // it should reject the TCP connection instead of responding successfully with the complete report.
            // Default: false
            options.RejectTcpConnectionWhenUnhealthy = true;

            // Override the default JSON health report serializer with your own.
            options.Serializer = new MyHealthReportSerializer();
        });

public class MyHealthReportSerializer : IHealthReportSerializer
{
    public byte[] Serialize(HealthReport healthReport)
    {
        return Array.Empty<byte>();
	}
}
```