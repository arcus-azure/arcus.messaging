---
title: "Message Pump Customization"
layout: default
---

# Customize message pumps

While the message processing is handled by the `IMessageHandler<>` implementations, the message pump controls in what format the message is received.
We allow several customizations while implementing your own message pump.

- [Control custom deserialization](#control-custom-deserialization)
- [Filter messages based on message context](#filter-messages-based-on-message-context)
- [Fallback message handling](#fallback-message-handling)

## Control custom deserialization

When inheriting from an `...MessagePump` type, there's a way to control how the incoming raw message is being deserialized.
Based on the message type of the registered message handlers, the pump determines if the incoming message can be deserialized to that type.

```csharp
public class OrderMessagePump : MessagePump
{
    public OrderMessagePump(
        IConfiguration configuration, 
        IServiceProvider serviceProvider, 
        ILogger<OrderMessagePump> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    public override bool TryDeserializeToMessageFormat(string message, Type messageType, out object? result)
    {
        if (messageType == typeof(Order))
        {
            result = JsonConvert.DeserializeObject<Order>(message);
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }
}
```

## Filter messages based on message context

When registering a new message handler, one can opt-in to add a filter on the message context which filters out messages that are not needed to be processed.

This can be useful when you are sending different message types on the same queue. Another use-case is being able to handle different versions of the same message type which have different contracts because you are migrating your application.

Following example shows how a message handler should only process a certain message when a property's in the context is present.

We'll use a simple message handler implementation:

```csharp
public class OrderMessageHandler : IMessageHandler<Order>
{
    public async Task ProcessMessageAsync(Order order, MessageContext context, ...)
    {
        // Do some processing...
	}
}
```

We would like that this handler only processed the message when the context contains `MessageType` equals `Order`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.WithMessageHandler<OrderMessageHandler, Order>(context => context.Properties["MessageType"].ToString() == "Order");
}
```

> Note that the order in which the message handlers are registered is important in the message processing.
> In the example, when a message handler above this one is registered that could also handle the message (same message type) than that handler may be chosen instead of the one with the specific filter.

## Filter messages based on message body

When registereing a new message handler, one can opt-in to add a filter on the incoming message body which filters out messages that are not needed to be processed.
This can be useful when you want to route messages based on the message content itself instead of the messaging context.

Following example shows how a message handler should only process a certain message when the status is 'Sales'; meaning only `Order` for the sales division will be processed.

```csharp
// Message to be sent:
public enum Department { Sales, Marketing, Operations }

public class Order
{
    public string Id { get; set; }
    public Department Type { get; set; }
}

// Message handler
public class OrderMessageHandler : IMessageHandler<Order>
{
    public async Task ProcessMessageAsync(Order order, MessageContext context, ...)
    {
        // Do some processing...
    }
}

// Message handler registration
public class Startup
{
    ...
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.WithMessageHandler<OrderMessageHandler, Order>((Order order) => order.Type == Department.Sales);
    }
}
```

## Fallback message handling

When receiving a message on the message pump and none of the registered `IMessageHandler`'s can correctly process the message, the message pump normally throws and logs an exception.

It could also happen in a scenario that's to be expected that some received messages will not be processed correctly (or you don't want them to).

In such a scenario, you can choose to register a `IFallbackMessageHandler` in the dependency container. 
This extra message handler will then process the remaining messages that can't be processed by the normal message handlers.

Following example shows how such a message handler can be implemented:

```csharp
public class WarnsUserFallbackMessageHandler : IFallbackMessageHandller
{
    private readonly ILogger _logger;

    public WarnsUserFallbackMessageHandler(ILogger<WarnsUserFallbackMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(string message, MessageContext context, ...)
    {
        _logger.LogWarning("These type of messages are expected not to be processed");
    }
}
```

And to register such an implementation:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.WithFallbackMessageHandler<WarnsUserFallbackMessageHandler>();
}
```

[&larr; back](/)
