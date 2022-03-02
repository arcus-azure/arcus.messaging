---
title: "TCP Health probe"
layout: default
---

# TCP Health probe

We provide a TCP health probe endpoint that allows a runtime to periodically check the liveness/readiness of the host based on the [.NET Core health checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks).

## Installation

This features requires to install our NuGet package:

```shell
PM > Install-Package Arcus.Messaging.Health
```

## Usage

To include the TCP endpoint, add the following line of code in the `Startup.ConfigureServices` method:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add TCP health probe without extra health checks.
        services.AddTcpHealthProbes("MyConfigurationKeyToTcpHealthPort");

        // Or, add your extra health checks in a configuration delegate.
        services.AddTcpHealthProbes(
            "MyConfigurationKeyToTcpHealthPort",
            configureHealthChecks: healthBuilder => 
            {
                healthBuilder.AddCheck("Example", () => HealthCheckResult.Healthy("Example is OK!"), tags: new[] { "example" })
            });
    }
}
```

## Configuration

The TCP probe allows several additional configuration options.

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add TCP health probe with or without extra health checks.
        services.AddTcpHealthProbes(
            "MyConfigurationKeyToTcpHealthPort",
            configureTcpListenerOptions: options =>
            {
                // Configure the configuration key on which the health report is exposed.
                options.TcpPortConfigurationKey = "MyConfigurationKey";

                // Configure how the health report should be serialized.
                options.HealthReportSerializer = new MyHealthReportSerializer();

                // Configure how the health report status should affect the TCP probe's availability.
                // When set to `true`, unhealthy health reports will result in rejecting of TCP client connection attempts.
                // When set to `false` (default), TCP client connection attempts will be accepted but the returned health report will have a unhealthy health status.
                options.RejectTcpConnectionWhenUnhealthy = true;
            },
            configureHealthCheckPublisherOptions: options =>
            {
                // Configures additional options regarding how fast or slow changes in the health report should affect the TCP probe's availability.
                // When the RejectTcpConnectionWhenUnhealthy is set to `true`.

                // The initial delay after the application starts with monitoring the health report changes.
                options.Delay = TimeSpan.Zero;

                // The interval in which the health report is monitored for changes.
                options.Period = TimeSpan.FromSeconds(5);

                // See https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.healthcheckpublisheroptions?view=dotnet-plat-ext-5.0 for more information.
            });
    }
}

using Arcus.Messaging.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class MyHealthReportSerializer : IHealthReportSerializer
{
    public byte[] Serialize(HealthReport healthReport)
    {
        return Array.Empty<byte>();
	}
}
```

[&larr; back](/)
