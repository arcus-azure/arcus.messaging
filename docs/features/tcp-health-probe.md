---
title: "TCP Health probe"
layout: default
---

# TCP Health probe

A `BackgroundService` can be added to the <span>ASP.NET</span> hosted services to expose a TCP health endpoint that allows a runtime to periodically check the liveness/readiness of the host.

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