---
title: "General messaging"
layout: default
---

# General messaging functionality
The Arcus Messaging library has several messaging systems that can retrieve messages from Azure technology (like Azure Service Bus and Azure EventHubs), and run abstracted message handlers on the received messages.
Both and future systems also support some general functionality that will be explained here.

## Stop message pump when downstream is unable to keep up

### Pause message processing with a circuit breaker
When your message handler interacts with a dependency on an external resource, that resource may become unavailable. In that case you want to temporarily stop processing messages.

⚠️ This functionality is currently only available for the Azure Service Bus message pump.

⚠️ This functionality is not supported for the Azure Event Hubs message pump.

⚠️ This functionality is only available when interacting with message pumps, not in message router-only scenarios like Azure Functions.

To interact with the message processing system within your custom message handler, you can inject the `IMessagePumpCircuitBreaker`:

```csharp
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Pumps.Abstractions.Resiliency;

public class OrderMessageHandler : CircuitBreakerServiceBusMessageHandler<Order>
{
    private readonly IMessagePumpCircuitBreaker _circuitBreaker;

    public OrderMessageHandler(IMessagePumpCircuitBreaker circuitBreaker, ILogger<...> logger) : base(circuitBreaker, logger)
    {
        _circuitBreaker = circuitBreaker;
    }

    public override async Task ProcessMessageAsync(
        Order message,
        AzureServiceBusMessageContext messageContext,
        MessagePumpCircuitBreakerOptions options,
        ...)
    {
        // Determine whether your dependent system is healthy...
        if (!IsDependentSystemHealthy())
        {
            throw new ...Exception("My dependency system is temporarily unavailable, please halt message processing for now");
        }
        else
        {
            // Process your message...
        }
    }
}
```

The message pump will by default act in the following pattern:
* Circuit breaker calls `Pause`
  * Message pump stops processing messages for a period of time (circuit is OPEN).
* Message pump tries processing a single message (circuit is HALF-OPEN).
  * Dependency still unhealthy? => circuit breaker pauses again (circuit is OPEN)
  * Dependency healthy? => circuit breaker resumes, message pump starts receiving message in full again (circuit is CLOSED).

Both the recovery period after the circuit is open and the interval between messages when the circuit is half-open is configurable when calling the circuit breaker. These time periods are related to your dependent system and could change by the type of transient connection failure.

```csharp
 public override async Task ProcessMessageAsync(
        Order message,
        AzureServiceBusMessageContext messageContext,
        MessagePumpCircuitBreakerOptions options,
        ...)
{
    // Sets the time period the circuit breaker should wait before retrying to receive messages.
    // A.k.a. the time period the circuit is closed (default: 30 seconds).
    options.MessageRecoveryPeriod = TimeSpan.FromSeconds(15);
 
    // Sets the time period the circuit breaker should wait between each message after the circuit was closed, during recovery.
    // A.k.a. the time interval to receive messages during which the circuit is half-open (default: 10 seconds).
    options.MessageIntervalDuringRecovery = TimeSpan.FromSeconds(1.5);
}
```

#### 🔔 Get notified on a circuit breaker state transition
Transitions from circuit breaker states happens internally and automatically. The library supports a notification system that lets you register event handlers that gets called upon a state transition.

After the message pump and/or message handlers are registered, you can add one or more handlers linked to specific transitions to the previously registered pump.

```csharp
using Arcus.Messaging.Pumps.Abstractions;

services.AddServiceBusMessagePump(...)
        .WithCircuitBreakerStateChangedEventHandler<MyFirstCircuitBreakerEventHandler>()
        .WithCircuitBreakerStateChangedEventHandler<MySecondCircuitBreakerEventHandler>();
```

The instances should implement the `ICircuitBreakerEventHandler`, which allows you to access the new state for the pump.

```csharp
public class MyFirstCircuitBreakerEventHandler : ICircuitBreakerEventHandler
{
    public Task OnTransitionAsync(MessagePumpCircuitStateChang change)
    {
        // The job ID of the message pump that was transitioned.
        string jobId = change.JobId;

        // The circuit breaker state transitions.
        MessagePumpCircuitState oldState = change.OldState;
        MessagePumpCircuitState newState = change.NewState;

        // Process the state change event...
    }
}
```

### Pause message processing for a fixed period of time 
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
