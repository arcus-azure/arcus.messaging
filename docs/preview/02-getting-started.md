---
sidebar_label: Getting started
---

# Getting started with Arcus Messaging
**Welcome to Arcus Messaging!** 🎉

This page is dedicated to be used as a walkthrough on how to set up Arcus Messaging in a new or existing project. Arcus Messaging is an umbrella term for a set of NuGet packages that kickstart your messaging solution.

**Used terms:**
* *message handler:* a custom implementation of an Arcus Messaging-provided interface that processes a deserialized Azure Service Bus message.
* *message pump:* an Arcus Messaging-provided registered service that receives Azure Service Bus messages for you, and "pumps" them through your *message handlers*.

## The basics
:::note
Arcus Messaging is currently only supported for [Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview) solutions.
:::

Arcus Messaging helps with receiving messages from a message broker. This walkthrough uses Azure Service Bus as an example.

Instead of directly interacting with the [`ServiceBusReceiver`](https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusreceiver), it allows you to implement one or more *'message handler'* interfaces. Arcus Messaging will use these custom implementations and determine based on several criteria to which  *'message handler'* it should route the message.

By using *'message handlers'*, you don't have to worry about routing, deserialization, or even complete/dead-letter/abandon messages - but you can take care of them as well.

![Message handling schema](/media/worker-message-handling.png)

## Implement your first message handler
First step in creating your message handler, is installing the following package. This is the only package that is required during this walkthrough.

```powershell
PS> Install-Package -Name Arcus.Messaging.Pumps.ServiceBus
```

The package makes the `IServiceBusMessageHandler<>` interface available. Implementing this interface is the simplest way of creating a *'message handler'*.

As the generic type, you can use the DTO (data-transfer object) to which the [`ServiceBusReceivedMessage.Body`](https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusreceivedmessage.body) should be deserialized to (default via JSON). In this case: `MyOrder`.

```csharp
using Arcus.Messaging.ServiceBus;

public class MyOrder
{
    public string OrderId { get; set; }
    public string ProductName { get; set; }
}

public class MyOrderMessageHandler : IServiceBusMessageHandler<MyOrder>
{
    public async Task ProcessMessageAsync(
        MyOrder order,
        ServiceBusMessageContext context,
        MessageCorrelationInfo correlation,
        CancellationToken cancellation)
    {
        // Process further your custom type...
    }
}
```

## Register your message handlers
A messaging solution usually has more than one type of message it receives. Differentiating between types, properties, can be a hassle to set up.

*'Message handlers'* are registered on a *'message pump'* in your application. This 'pump' receives the Azure Service Bus messages for you and routes them to the right handler.

```csharp
using Microsoft.Extensions.DependencyInjection;

Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        // highlight-start
        services.AddServiceBusQueueMessagePump("<queue-name>", "<namespace>", new ManagedIdentityCredential())
                .WithServiceBusMessageHandler<MyOrderMessageHandler, MyOrder>()
                .WithServiceBusMessageHandler<MyOrderV2MessageHandler, MyOrderV2>();
        // highlight-end
    })
    .Build()
    .Run();
```

The way *'message handlers'* are registered determines when the received message will be routed to them.

> 🔗 See the [Azure Service Bus messaging feature documentation](./03-Features/01-Azure/01-service-bus.mdx) for more information on providing additional routing filters to your message handlers.