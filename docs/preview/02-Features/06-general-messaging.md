---
title: "General messaging"
layout: default
---

# General messaging functionality
The Arcus Messaging library has several messaging systems that can retrieve messages from Azure technology (like Azure Service Bus and Azure EventHubs), and run abstracted message handlers on the received messages.
Both and future systems also support some general functionality that will be explained here.

## Stop message pump when downstream is unable to keep up

⚡ If you use one of the message-type specific packages like `Arcus.Messaging.Pumps.EventHubs`, you will automatically get this functionality. If you implement your own message pump, please use the `services.AddMessagePump(...)` extension which makes sure that you also registers this functionality.

When messages are being processed by a system that works slower than the rate that messages are being received by the message pump, a rate problem could occur. 
As a solution to this problem, the Arcus Messaging library registers an `IMessagePumpLifetime` instance in the application services that lets you control the message receiving process and pauses if necessary for the downstream dependency system to keep up.

The following example below shows how a 'rate limit' service gets injected with the `IMessagePumpLifetime` instance and pauses.
Note that the message pumps need to be registered with a unique job ID so that the lifetime component knows which pump it needs to manage.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Arcus.Messaging.Pumps.Abstractions;

public class Program
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddServiceBusMessagePump(..., options => options.JobId = "abc-123")
                .WithServiceBusMessageHandler<..., ...>();

        services.AddEventHubsMessagePump(..., options => options.JobId = "def-456")
                .WithEventHubsMessageHandler<..., ...>();
    }
}

public class RateLimitService
{
    private readonly IMessagePumpLifetime _pumpLifetime;

    public RateLimitService(IMessagePumpLifetime lifetime)
    {
        _pumpLifetime = lifetime;
    }

    public async Task CantKeepUpAnymoreAsync(CancellationToken cancellationToken)
    {
        var duration = TimeSpan.FromSeconds(30);
        await _pumpLifetime.PauseProcessingMessagesAsync("abc-123", duration, cancellationToken);
    }
}
```

⚡ Besides the `PauseProcessingMessagesAsync` method, there also exists `Stop...`/`Start...` variants so that you can control the time dynamically when the pump is allowed to run again.

For more information on message pumps:
- [Azure Service Bus message pump](./02-message-handling/01-service-bus.md)
- [Azure EventHubs message pump](./02-message-handling/03-event-hubs.md)
