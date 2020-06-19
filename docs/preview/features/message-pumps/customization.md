---
title: "Message Pump Customization"
layout: default
---

# Customize message pumps

While the message processing is handled by the `IMessageHandler<>` implementations, the message pump controls in what format the message is received.
We allow several customizations while implementing your own message pump.

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

[&larr; back](/)
