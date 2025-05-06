---
sidebar_label: Service Bus
---

# Azure Service Bus messaging
The `Arcus.Messaging.Pumps.ServiceBus` library provides a way to process Azure Service Bus messages on queues/topic subscriptions via custom routed *message handlers*, instead of interacting with the [`ServiceBusReceiver`](https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusreceiver) yourself. 

> 🔗 See the [getting started page](../../02-getting-started.md) to understand the 'message handler' and 'message pump' concepts.

## Installation
This features requires to install our NuGet package:

```powershell
PS> Install-Package -Name Arcus.Messaging.Pumps.ServiceBus
```

## Implementing a message handler
To receive the Azure Service Bus message in a deserialized form, you can implement one or more *message handlers*, each with the expected DTO (data-transfer object) that the [`ServiceBusReceivedMessage.Body`](https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusreceivedmessage.body) will be deserialized towards (default via JSON).

Here is an example of such a message handler that expects messages of type `Order`:

```csharp
using Arcus.Messaging.Abstractions.ServiceBus.MessagingHandling;

public class Order
{
    public string OrderId { get; set; }
    public string ProductName { get; set; }
}

public class OrderMessageHandler : IAzureServiceBusMessageHandler<Order>
{
    private readonly ILogger _logger;

    // Provide any number of dependencies that are available in the application services:
    public OrderMessageHandler(ILogger<OrdersMessageHandler> logger)
    {
        _logger = logger;
    }

    // Directly interact with your custom deserialized model (in this case 'Order'): 
    public async Task ProcessMessageAsync(
        Order order, 
        AzureServiceBusMessageContext context, 
        MessageCorrelationInfo correlation, 
        CancellationToken cancellationToken)
    {
        // Process your custom order...
    }
}
```

## Registering your message handlers
All your custom *message handlers* need to be registered on a *message pump*. This "pump" is an Arcus Messaging-provided service that receives the Azure Service Bus messages for you, and "pumps" them to the right *message handler*.

### Register the Arcus message pump
There exists two types of Azure Service Bus *message pumps*: for queues and for topic subscriptions. During the registration of the pump in the application services, the type of authentication mechanism can be configured.

> 🎖️ Use the [`ManagedIdentityCredential`](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.managedidentitycredential) where possible.

```csharp
using Microsoft.Extensions.DependencyInjection;

Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        // Register the message pump via a `TokenCredential` implementation.
        // 🔗 See Microsoft's guidance on Azure authentication: 
        // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme
        services.AddServiceBusQueueMessagePump(
            "<queue-name>",
            "<fully-qualified-namespace>",
            new ManagedIdentityCredential());


        // Register the message pump via a custom implementation factory, returning a `ServiceBusClient`.
        // 🔗 See Microsoft's guidance on Azure core extensions to re-use clients in your application:
        // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/microsoft.extensions.azure-readme
        services.AddServiceBusTopicMessagePump(
            "<topic-name>",
            "<subscription-name>",
            "<fully-qualified-namespace>",
            (IServiceProvider services) =>
            {
                return services.GetRequiredService<ServiceBusClient>();
            });
    });
```

### Register your message handlers on message pump
Your custom *message handlers* can be registered on a *message pump* registration. Both the type of the handler and the type of messages it can process, is required.

```csharp
using Microsoft.Extensions.DependencyInjection;

Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddServiceBusTopicMessagePump(...)
                .WithServiceBusMessageHandler<OrderMessageHandler, Order>()
                .WithServiceBusMessageHandler<OrderV2MessageHandler, OrderV2>();
    });
```

> **⚠️ Considerations:**
> * The order in which the *message handlers* are registered matters when a message is routed.
> * Only the first matching *message handler* will be used to process the message, even when multiple match.
> * Multiple *message handlers* with the same (message) type can registered, but they need to distinguish themselves with [routing options](#message-handler-routing-customization).

## Customization
Due to the wide range of situations within messaging solutions, Arcus Messaging supports highly customizable message pump/handler registrations.

### Message pump customization
The following options are available when registering the Azure Service Bus message pump.

```csharp
services.AddServiceBus[Topic/Queue]MessagePump(..., options => 
{
    // Indicate whether or not messages should be automatically marked as completed 
    // if no exceptions occurred and processing has finished (default: true).
    options.AutoComplete = false;

    // The amount of concurrent calls to process messages 
    // (default: null, leading to the defaults of the Azure Service Bus SDK message handler options).
    options.MaxConcurrentCalls = 5;

    // Specifies the amount of messages that will be eagerly requested during processing.
    // Setting the PrefetchCount to a value higher then the MaxConcurrentCalls value helps 
    // maximizing throughput by allowing the message pump to receive from a local cache rather then waiting on a service request.
    options.PrefetchCount = 10;

    // The unique identifier for this background job to distinguish 
    // this job instance in a multi-instance deployment (default: generated GUID).
    options.JobId = Guid.NewGuid().ToString();

    // Indicate whether or not the default built-in JSON deserialization should ignore additional members 
    // when deserializing the incoming message (default: AdditionalMemberHandling.Error).
    options.Routing.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore;
});
```

### Message handler routing customization
The following routing options are available when registering an Azure Service Bus message handler on a message pump.

> 💡 Setting one or more of the routing options helps the message pump to better select the right *message handler* for the received message.

```csharp
services.AddServiceBus[Topic/Queue]MessagePump(...)
        .WithServiceBusMessageHandler<OrderMessageHandler, Order>(..., options =>
        {
            // Adds a filter to the message handler registration:
            // Only messages with 'Type=Order' property goes through this message handler.
            options.AddMessageContextFilter(ctx => ctx.Properties["Type"] == "Order");

            // Adds a filter to the message handler registration:
            // Only messages with certain bodies goes through this message handler.
            options.AddMessageBodyFilter((Order order) => order.OrderId = "123");

            // Adds a custom message deserializer to the message handler registration:
            // Only messages that gets deserialized successfully goes through this message handler.
            // 👉 Custom implementations of the `Arcus.Messaging.Abstractions.MessageHandling.IMessageBodySerializer` interface.
            //      public interface IMessageBodySerializer
            //      {
            //          Task<MessageResult> DeserializeMessageAsync(string messageBody);
            //      }
            options.AddMessageBodySerializer(new CustomXmlMessageBodySerializer());
        });
```

### Custom message settlement
When messages can't be matched to any of your custom registered message handlers, Arcus will dead-letter the message. When one of your message handlers fails to process a message, it will get abandoned and maybe only after a third try be dead-lettered.

This settlement of received Azure Service Bus messages can also be customized by calling one of the Service Bus operations yourself via the message context.

```csharp
public class OrderMessageHandler : IAzureServiceBusMessageHandler<Order>
{
    public async Task Task ProcessMessageAsync(
        Order message,
        AzureServiceBusMessageContext messageContext,
        MessageCorrelationInfo correlation,
        CancellationToken cancellation)
    {
        await messageContext.DeadLetterMessageAsync("Reason: Unsupported", "Message type is not supported");
    }
}
```

The following operations are supported:
* **Dead-letter**
* **Abandon**
* **Complete**

### Pause message processing with a circuit breaker
When your message handler interacts with an external dependency, that dependency may become unavailable. In that case you want to temporarily stop processing messages.

To interact with the message processing system within your *message handler*, you can inherit from the `CircuitBreakerServiceBusMessageHandler<>`, which allows you to 'enrich' your handler with circuit-breaker functionality.

```csharp
using Arcus.Messaging.Pumps.Abstractions.Resiliency;

public class OrderMessageHandler : CircuitBreakerServiceBusMessageHandler<Order>
{
    private readonly IMessagePumpCircuitBreaker _circuitBreaker;

    public OrderMessageHandler(
        IMessagePumpCircuitBreaker circuitBreaker,
        ILogger<OrderMessageHandler> logger) : base(circuitBreaker, logger)
    {
        _circuitBreaker = circuitBreaker;
    }

    public override async Task ProcessMessageAsync(
        Order message,
        AzureServiceBusMessageContext context,
        MessageCorrelationInfo correlation,
        MessagePumpCircuitBreakerOptions options,
        CancellationToken cancellation)
    {
        // Determine whether your dependent system is healthy...
        if (!IsMyDependentSystemHealthy())
        {
            // Let the message processing fail with a custom exception.
            throw new MyDependencyUnnavailableException("My dependency system is temporarily unavailable, please halt message processing for now");
        }
    }
}
```

> The circuit-breaker functionality will follow this pattern when the handler lets the message processing fail:
> * Message processing pause for a **recovery period** of time (circuit=OPEN).
> * Message processing tries a single message (circuit=HALF-OPEN).
>   * Message handler still throws exception? => message processing pauses for an **interval** and tries again (circuit=OPEN).
>   * Message handler stops throwing exception? => message processing resumes in full (circuit=CLOSED).

Both the recovery period after the circuit is open and the interval between messages when the circuit is half-open is configurable with the `MessagePumpCircuitBreakerOptions`.

```csharp
 public override async Task ProcessMessageAsync(..., MessagePumpCircuitBreakerOptions options)
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
To get notified on circuit-breaker state transitions, you can register one or more event handlers on a message pump.

These event handlers should implement the `ICircuitBreakerEventHandler` interface:

```csharp
public class MyFirstCircuitBreakerEventHandler : ICircuitBreakerEventHandler
{
    public Task OnTransitionAsync(MessagePumpCircuitStateChangedEventArgs args)
    {
        // The job ID of the message pump that was transitioned.
        string jobId = change.JobId;

        // The circuit breaker state transitions.
        MessagePumpCircuitState oldState = change.OldState;
        MessagePumpCircuitState newState = change.NewState;
    }
}
```

Once implemented, these can be registered on a message pump:

```csharp
using Microsoft.Extensions.DependencyInjection;

services.AddServiceBus[Queue/Topic]MessagePump(...)
        .WithCircuitBreakerStateChangedEventHandler<MyFirstCircuitBreakerEventHandler>()
        .WithCircuitBreakerStateChangedEventHandler<MySecondCircuitBreakerEventHandler>();
```