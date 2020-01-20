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
public void ConfigureServices(IServiceCollection services)
{
    // Add TCP health probe without extra health checks.
    services.AddTcpHealthProbes("MyConfigurationKeyToTcpHealthPort");

    // Or, add your extra health checks in a configuration delegate.
    services.AddTcpHealthProbes(
        "MyConfigurationkeyToTcpHealthPort",
        configureHealthChecks: healthBuilder => 
        {
            healthBuilder.AddCheck("Example", () => HealthCheckResult.Healthy("Example is OK!"), tags: new[] { "example" })
        });
}
```

## Configuration

The TCP probe allows several additional configuration options.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add TCP health probe with or whitout extra health checks.
    services.AddTcpHealthProbes(
        "MyConfigurationKeyToTcpHealthPort",
        configureTcpListenerOptions: options =>
        {
            // Configure the configuration key on which the health report is exposed.
            options.TcpPortConfigurationKey = "MyConfigurationKey";

            // Configure how the health report should be serialized.
            options.HealthReportSerializer = new MyHealthReportSerializer();
        });
}

public class MyHealthReportSerializer : IHealthReportSerializer
{
    public byte[] Serialize(HealthReport healthReport)
    {
        return Array.Empty<byte>();
	}
}
```

[&larr; back](/)
