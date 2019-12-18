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
    services.AddTcpHealthProbes();

    // Or, add your extra health checks in a configuration delegate.
    services.AddTcpHealthProbes(healthBuilder => 
    {
        healthBuilder.AddCheck("Example", () => HealthCheckResult.Healthy("Example is OK!"), tags: new[] { "example" })
    });
}
```

## Configuration

To make the TCP health check fully functional, you'll  require some configuration values. 

| Configuration key   | Usage        | Type  | Description                                                         |
| ------------------- | ------------ | ----- | ------------------------------------------------------------------- |
| `ARCUS_HEALTH_PORT` | **required** | `int` | The TCP port on which the health report is exposed.                 |

[&larr; back](/)
