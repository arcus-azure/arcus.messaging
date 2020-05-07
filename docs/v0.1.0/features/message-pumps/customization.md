---
title: "Message Pump Customization"
layout: default
---

# Customize Message Pumps

While the message processing is handled by the `IMessageHandler<>` implementations, the message pump controls in what format the message is received.
We allow several customizations while implementing your own message pump.

## Control Custom Deserialization

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

[&larr; back](/)