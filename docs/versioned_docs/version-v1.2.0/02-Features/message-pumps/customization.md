---
title: "Message Pump Customization"
layout: default
---

# Customize message pumps

While the message processing is handled by the `IMessageHandler<>` implementations, the message router controls in what format the message is received.
We allow several customizations while using a built-in or implementing your own message router.

- [Customize message pumps](#customize-message-pumps)
  - [Filter messages based on message context](#filter-messages-based-on-message-context)
  - [Control custom deserialization](#control-custom-deserialization)
    - [Extending the message router](#extending-the-message-router)
    - [Bring your own deserialization](#bring-your-own-deserialization)
  - [Filter messages based on message body](#filter-messages-based-on-message-body)
  - [Fallback message handling](#fallback-message-handling)

## Filter messages based on message context

When registering a new message handler, one can opt-in to add a filter on the message context which filters out messages that are not needed to be processed.

This can be useful when you are sending different message types on the same queue. Another use-case is being able to handle different versions of the same message type which have different contracts because you are migrating your application.

Following example shows how a message handler should only process a certain message when a property's in the context is present.

We'll use a simple message handler implementation:

```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessagingHandling;

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
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.WithMessageHandler<OrderMessageHandler, Order>(context => context.Properties["MessageType"].ToString() == "Order");
    }
}
```

> Note that the order in which the message handlers are registered is important in the message processing.
> In the example, when a message handler above this one is registered that could also handle the message (same message type) than that handler may be chosen instead of the one with the specific filter.

## Control custom deserialization

### Extending the message router

By default, when using the `.AddMessageRouting`, an new `IMessageRouter` instance is registered that contains the basic message routing/handling for incoming messages.

If you want to control the message routing or provide additional functionality, you can do this by implementing your own message router. Next to that, you can also inherit from our built-in message router and override the necessary methods of your choosing.

Here's how you normally would register the message router:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Registers `IMessageRouter`.
    services.AddMessageRouting();
}
```

Alternatively, you can implement your own.

```csharp
using System;
using Arcus.Messaging.Pumps.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class OrderMessageRouter : MessageRouter
{
    public OrderMessageRouter(
        IServiceProvider serviceProvider, 
        ILogger<OrderMessageRouter> logger)
        : base(serviceProvider, logger)
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

> Note that our built-in `MessageRouter` implements `IMessageRouter` which you can use within your application.

When inheriting from the `MessageRouter` type, we've controlled how the incoming raw message is being deserialized.

Based on the message type of the registered message handlers, the router determines if the incoming message can be deserialized to that type or not.

This custom message router can be registered using the following code. The custom router will then be registered as an `IMessageRouter` instance.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMessageRouting(serviceProvider =>
    {
        var logger = serviceProvider.GetService<ILogger<OrderMessageRouter>>();
        return new OrderMessageRouter(serviceProvider, logger);
    });
}
```

### Bring your own deserialization

You can also choose to extend the built-in message deserialization with a custom deserializer to meet your needs. 
This allows you to easily deserialize into different message formats or reuse existing (de)serialization capabilities that you already have without altering the message router. 

You start by implementing an `IMessageBodySerializer`. The following example shows how an expected type can be transformed to something else. 
The result type (in this case `OrderBatch`) will be then be used to check if there is an `IMessageHandler` registered with that message type.

```csharp
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;

public class OrderBatchMessageBodySerializer : IMessageBodySerializer
{
    public async Task<MessageResult> DeserializeMessageAsync(string messageBody)
    {
        var serializer = new XmlSerializer(typeof(Order[]));
        using (var contents = new MemoryStream(Encoding.UTF8.GetBytes(messageBody)))
        {
            var orders = (Order[]) serializer.Deserialize(contents);
            return MessageResult.Success(new OrderBatch(orders));
        }
    }
}
```

The registration of these message body handlers can be done just as easily as an `IMessageSerializer`:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the message body serializer in the dependency container where the dependent services will be injected.
        services.WithMessageHandler<OrderBatchMessageHandler>(..., messageBodySerializer: new OrderBatchMessageBodySerializer());

        // Register the message body serializer  in the dependency container where the dependent services are manually injected.
        services.WithMessageHandler(..., messageBodySerializerImplementationFactory: serviceProvider => 
        {
            var logger = serviceProvider.GetService<ILogger<OrderBatchMessageHandler>>();
            return new OrderBatchMessageHandler(logger);
        });
    }
}
```

> Note that the order in which the message handlers are registered is important in the message processing.
> In the example, when a message handler above this one is registered that could also handle the message (same message type) than that handler may be chosen instead of the one with the specific filter.

## Filter messages based on message body

When registering a new message handler, one can opt-in to add a filter on the incoming message body which filters out messages that are not needed to be processed by this message handler.
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

using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;

// Message handler
public class OrderMessageHandler : IMessageHandler<Order>
{
    public async Task ProcessMessageAsync(Order order, MessageContext context, ...)
    {
        // Do some processing...
    }
}

using Microsoft.Extensions.DependencyInjection;

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

When receiving a message on the message pump and none of the registered `IMessageHandler`'s can correctly process the message, the message router normally throws and logs an exception.

It could also happen in a scenario that's to be expected that some received messages will not be processed correctly (or you don't want them to).

In such a scenario, you can choose to register a `IFallbackMessageHandler` in the dependency container. 
This extra message handler will then process the remaining messages that can't be processed by the normal message handlers.

Following example shows how such a message handler can be implemented:

```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Microsoft.Extensions.Logging;

public class WarnsUserFallbackMessageHandler : IFallbackMessageHandler
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
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.WithFallbackMessageHandler<WarnsUserFallbackMessageHandler>();
    }
}
```

[&larr; back](/)
