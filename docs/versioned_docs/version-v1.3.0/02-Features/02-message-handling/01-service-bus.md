---
title: "Azure Service Bus message handling"
layout: default
---

# Azure Service Bus message handling
The `Arcus.Messaging.Pumps.ServiceBus` library provides ways to perform all the plumbing that is required for processing messages on queues and topic subscriptions. 

As a user, the only thing you have to do is **focus on processing messages, not how to get them**. Following terms are used:
- **Message handler**: implementation that processes the received message from an Azure Service Bus queue or topic subscription. Message handlers are created by implementing the `IAzureServiceBusMessageHandler<TMessage>`. This message handler will be called upon when a message is available in the Azure Service Bus queue or on the topic subscription. [this section](#message-handler-example) for a message handler example setup
- **Message router**: implementation that delegates the received Azure Service Bus message to the correct message handler. For alternative message routing, see [this section](#alternative-service-bus-message-routing) for more information.
- **Message pump**: implementation that interacts and receives the Azure Service Bus message. The pump can be configured for different scenarios, see [this section](#pump-configuration) for more information.

![Message handling schema](/media/worker-message-handling.png)

## Installation
This features requires to install our NuGet package:

```shell
PM > Install-Package Arcus.Messaging.Pumps.ServiceBus
```

> ⚠ The new Azure SDK doesn't yet support Azure Service Bus plugins. See this [migration guide](https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/servicebus/Azure.Messaging.ServiceBus/MigrationGuide.md#known-gaps-from-previous-library) for more info on this topic.

## Message handler example
Here is an example of a message handler that expects messages of type `Order`:

```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Microsoft.Extensions.Logging;

public class OrdersMessageHandler : IAzureServiceBusMessageHandler<Order>
{
    private readonly ILogger _logger;

    public OrdersMessageHandler(ILogger<OrdersMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        Order orderMessage, 
        AzureServiceBusMessageContext messageContext, 
        MessageCorrelationInfo correlationInfo, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber, orderMessage.Customer.FirstName, orderMessage.Customer.LastName);

        // Process the message.

        _logger.LogInformation("Order {OrderId} processed", orderMessage.Id);
    }
}
```

## Message handler registration
Once the message handler is created, you can very easily register it:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add Service Bus Queue message pump and use OrdersMessageHandler to process the messages
        // - ISecretProvider will be used to lookup the connection string scoped to the queue for secret ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING
        services.AddServiceBusQueueMessagePump("ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING")
                .WithServiceBusMessageHandler<OrdersMessageHandler, Order>();

        // Add Service Bus Topic message pump and use OrdersMessageHandler to process the messages on the 'My-Subscription-Name' subscription
        // - Topic subscriptions over 50 characters will be truncated
        // - ISecretProvider will be used to lookup the connection string scoped to the queue for secret ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING
        services.AddServiceBusTopicMessagePump("My-Subscription-Name", "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING")
                .WithServiceBusMessageHandler<OrdersMessageHandler, Order>();

        // Note, that only a single call to the `.WithServiceBusMessageHandler` has to be made when the handler should be used across message pumps.
    }
}
```

In this example, we are using the Azure Service Bus message pump to process a queue and a topic and use the connection string stored in the `ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING` connection string.

> 💡 We support **connection strings that are scoped on the Service Bus namespace and entity** allowing you to choose the required security model for your applications. If you are using namespace-scoped connection strings you'll have to pass your queue/topic name as well.

### Filter messages based on message context
When registering a new message handler, one can opt-in to add a filter on the message context which filters out messages that are not needed to be processed.

This can be useful when you are sending different message types on the same queue. Another use-case is being able to handle different versions of the same message type which have different contracts because you are migrating your application.

Following example shows how a message handler should only process a certain message when a property in the context has a specific value.

We'll use a simple message handler implementation:

```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessagingHandling;

public class OrderMessageHandler : IAzureServiceBusMessageHandler<Order>
{
    public async Task ProcessMessageAsync(Order order, AzureServiceBusMessageContext context, ...)
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
        services.WithServiceBusMessageHandler<OrderMessageHandler, Order>(context => context.Properties["MessageType"].ToString() == "Order");
    }
}
```

> Note that the order in which the message handlers are registered is important in the message processing.
> In the example, when a message handler above this one is registered that could also handle the message (same message type) than that handler may be chosen instead of the one with the specific filter.


### Bring your own deserialization
You can also choose to extend the built-in message deserialization with a custom deserializer to meet your needs. 
This allows you to easily deserialize into different message formats or reuse existing (de)serialization capabilities that you already have without altering the message router. 

You start by implementing an `IMessageBodySerializer`. The following example shows how an expected type can be transformed to something else. 
The result type (in this case `OrderBatch`) will then be used to check if there is an `IAzureServiceBusMessageHandler` registered for that message type.

```csharp
using Arcus.Messaging.Abstractions.MessageHandling;

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

The registration of these message body serializers can be done just as easily as an `IAzureServiceBusMessageHandler`:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the message body serializer in the dependency container where the dependent services will be injected.
        services.WitServiceBusMessageHandler<OrderBatchMessageHandler>(..., messageBodySerializer: new OrderBatchMessageBodySerializer());

        // Register the message body serializer  in the dependency container where the dependent services are manually injected.
        services.WithServiceBusMessageHandler(..., messageBodySerializerImplementationFactory: serviceProvider => 
        {
            var logger = serviceProvider.GetService<ILogger<OrderBatchMessageHandler>>();
            return new OrderBatchMessageHandler(logger);
        });
    }
}
```

> Note that the order in which the message handlers are registered is important in the message processing.
> In the example, when a message handler above this one is registered that could also handle the message (same message type) than that handler may be chosen instead of the one with the specific filter.

### Filter messages based on message body
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
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;

// Message handler
public class OrderMessageHandler : IAzureServiceBusMessageHandler<Order>
{
    public async Task ProcessMessageAsync(Order order, AzureServiceBusMessageContext context, ...)
    {
        // Do some processing...
    }
}

using Microsoft.Extensions.DependencyInjection;

// Message handler registration
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.WithServiceMessageHandler<OrderMessageHandler, Order>((Order order) => order.Type == Department.Sales);
    }
}
```

### Fallback message handling
When receiving a message on the message pump and none of the registered `IAzureServiceBusMessageHandler`'s can correctly process the message, the message pump normally throws and logs an exception.
It could also happen in a scenario that's to be expected that some received messages will not be processed correctly (or you don't want them to).

In such a scenario, you can choose to register a `IAzureServiceBusFallbackMessageHandler` in the dependency container. 
This extra message handler will then process the remaining messages that can't be processed by the normal message handlers.

Following example shows how such a message handler can be implemented:

```csharp
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

public class WarnsUserFallbackMessageHandler : IAzureServiceBusFallbackMessageHandler
{
    private readonly ILogger _logger;

    public WarnsUserFallbackMessageHandler(ILogger<WarnsUserFallbackMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(Message message, AzureServiceBusMessageContext context, ...)
    {
        _logger.LogWarning("These type of messages are expected not to be processed");
    }
}
```

> 💡 Note that you have access to the Azure Service Bus message and the specific message context. These can be used to eventually call `.Abandon()` on the message. See [this section](#influence-handling-of-service-bus-message-in-message-handler) for more information.

And to register such an implementation:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddServiceBusQueueMessagePump(...)
                .WithServiceBusFallbackMessageHandler<WarnsUserFallbackMessageHandler>();
    }
}
```

## Influence handling of Service Bus message in message handler
When an Azure Service Bus message is received (either via regular message handlers or fallback message handlers), we allow specific Azure Service Bus operations during the message handling.
Currently we support [**Dead letter**](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dead-letter-queues) and [**Abandon**](https://docs.microsoft.com/en-us/dotnet/api/microsoft.servicebus.messaging.messagereceiver.abandon?view=azure-dotnet).

### During (regular) message handling
To have access to the Azure Service Bus operations, you have to implement the `abstract` `AzureServiceBusMessageHandler<T>` class. 
Behind the screens it implements the `IMessageHandler<>` interface, so you can register this the same way as your other regular message handlers.

This base class provides several protected methods to call the Azure Service Bus operations:
- `.CompleteMessageAsync`
- `.DeadLetterMessageAsync`
- `.AbandonMessageAsync`

Example:

```csharp
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Microsoft.Extensions.Logging;

public class AbandonsUnknownOrderMessageHandler : AzureServiceBusMessageHandler<Order>
{
    public AbandonsUnknownOrderMessageHandler(ILogger<AbandonsUnknownOrderMessageHandler> logger)
        : base(logger)
    {
    }

    public override async Task ProcessMessageAsync(Order order, AzureServiceBusMessageContext context, ...)
    {
        if (order.Id < 1)
        {
            await AbandonMessageAsync();
        }
        else
        {
            Logger.LogInformation("Received valid order");
        }
    }
}
```

The registration happens the same as any other regular message handler:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddServiceBusQueueMessagePump(...)
                .WithServiceBusMessageHandler<AbandonUnknownOrderMessageHandler, Order>();
    }
}
```

### During fallback message handling
To have access to the Azure Service Bus operations, you have to implement the abstract `AzureServiceBusFallbackMessageHandler` class.
Behind the scenes it implements the `IServiceBusFallbackMessageHandler`, so you can register this the same way as any other fallback message handler.

This base class provides several protected methods to call the Azure Service Bus operations:
- `.CompleteAsync`
- `.DeadLetterAsync`
- `.AbandonAsync`

Example:

```csharp
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.ServiceBus.Abstractions.MessageHandling;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

public class DeadLetterFallbackMessageHandler : AzureServiceBusFallbackMessageHandler
{
    public DeadLetterFallbackMessageHandler(ILogger<DeadLetterFallbackMessageHandler> logger)
        : base(logger)
    {
    }

    public override async Task ProcessMessageAsync(ServiceBusReceivedMessage message, AzureServiceBusMessageContext context, ...)
    {
        Logger.LogInformation("Message is not handled by any message handler, will dead letter");
        await DeadLetterMessageAsync(message);
    }
}
```

The registration happens the same way as any other fallback message handler:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddServiceBusQueueMessagePump(...)
                .WithServiceBusFallbackMessageHandler<DeadLetterFallbackMessageHandler>();
    }
}
```

## Pump Configuration
Next to that, we provide a **variety of overloads** to allow you to:
- Specify the name of the queue/topic
- Only provide a prefix for the topic subscription, so each topic message pump is handling messages on separate subscriptions
- Configure how the message pump should work *(ie. max concurrent calls & auto delete)*
- Read the connection string from the configuration *(although we highly recommend using the [Arcus secret store](https://security.arcus-azure.net/features/secret-store) instead)*

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Specify the name of the Service Bus Queue:
        services.AddServiceBusQueueMessagePump(
            "My-Service-Bus-Queue-Name",
            "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING");

        // Specify the name of the Service Bus Topic, and provide a name for the Topic subscription:
        services.AddServiceBusMessageTopicMessagePump<OrdersMessageHandler>(
            "My-Service-Bus-Topic-Name",
            "My-Service-Bus-Topic-Subscription-Name",
            "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING");

        // Specify a topic subscription prefix instead of a name to separate topic message pumps.
        services.AddServiceBusTopicMessagePumpWithPrefix(
            "My-Service-Bus-Topic-Name"
            "My-Service-Bus-Subscription-Prefix",
            "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING");

        // Uses managed identity to authenticate with the Service Bus Topic:
        services.AddServiceBusTopicMessagePumpUsingManagedIdentity(
            topicName: properties.EntityPath,
            subscriptionName: "Receive-All", 
            fullyQualifiedNamespace: "<your-namespace>.servicebus.windows.net"
            // The optional client id to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
            // https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm
            clientId: "<your-client-id>");

        services.AddServiceBusTopicMessagePump(
            "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING",
            options => 
            {
                // Indicate whether or not messages should be automatically marked as completed 
                // if no exceptions occurred and processing has finished (default: true).
                options.AutoComplete = true;

                // Indicate whether or not the message pump should emit security events (default: false).
                options.EmitSecurityEvents = true;

                // The amount of concurrent calls to process messages 
                // (default: null, leading to the defaults of the Azure Service Bus SDK message handler options).
                options.MaxConcurrentCalls = 5;

                // The unique identifier for this background job to distinguish 
                // this job instance in a multi-instance deployment (default: guid).
                options.JobId = Guid.NewGuid().ToString();

                // The name of the Azure Service Bus message property that has the transaction ID.
                // (default: Transaction-Id).
                options.Correlation.TransactionIdPropertyName = "X-Transaction-ID";

                // The name of the Azure Service Bus message property that has the upstream service ID.
                // (default: Operation-Parent-Id).
                options.Correlation.OperationParentIdPropertyName = "X-Operation-Parent-ID";

                // The property name to enrich the log event with the correlation information cycle ID.
                // (default: CycleId)
                options.CorrelationEnricher.CycleIdPropertyName = "X-CycleId";

                // Indicate whether or not the default built-in JSON deserialization should ignore additional members 
                // when deserializing the incoming message (default: AdditionalMemberHandling.Error).
                options.Deserialization.AdditionalMembers = AdditionalMemberHandling.Ignore;

                // Indicate whether or not a new Azure Service Bus Topic subscription should be created/deleted
                // when the message pump starts/stops (default: CreateOnStart & DeleteOnStop).
                options.TopicSubscription = TopicSubscription.CreateOnStart | TopicSubscription.DeleteOnStop;
            });

        services.AddServiceBusQueueMessagePump(
            "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING",
            options => 
            {
                // Indicate whether or not messages should be automatically marked as completed 
                // if no exceptions occurred and processing has finished (default: true).
                options.AutoComplete = true;

                // Indicate whether or not the message pump should emit security events (default: false).
                options.EmitSecurityEvents = true;

                // The amount of concurrent calls to process messages 
                // (default: null, leading to the defaults of the Azure Service Bus SDK message handler options).
                options.MaxConcurrentCalls = 5;

                // The unique identifier for this background job to distinguish 
                // this job instance in a multi-instance deployment (default: guid).
                options.JobId = Guid.NewGuid().ToString();

                // The name of the Azure Service Bus message property that has the transaction ID.
                // (default: Transaction-Id).
                options.Correlation.TransactionIdPropertyName = "X-Transaction-ID";

                // The name of the Azure Service Bus message property that has the upstream service ID.
                // (default: Operation-Parent-Id).
                options.Correlation.OperationParentIdPropertyName = "X-Operation-Parent-ID";

                // The property name to enrich the log event with the correlation information cycle ID.
                // (default: CycleId)
                options.CorrelationEnricher.CycleIdPropertyName = "X-CycleId";

                // Indicate whether or not the default built-in JSON deserialization should ignore additional members 
                // when deserializing the incoming message (default: AdditionalMemberHandling.Error).
                options.Deserialization.AdditionalMembers = AdditionalMembersHandling.Ignore;
            });

        // Uses managed identity to authenticate with the Service Bus Topic:
        services.AddServiceBusQueueMessagePumpUsingManagedIdentity(
            queueName: "orders",
            serviceBusNamespace: "<your-namespace>"
            // The optional client id to authenticate for a user assigned managed identity. More information on user assigned managed identities cam be found here:
            // https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#how-a-user-assigned-managed-identity-works-with-an-azure-vm
            clientId: "<your-client-id>");

        // Multiple message handlers can be added to the services, based on the message type (ex. 'Order', 'Customer'...), 
        // the correct message handler will be selected.
        services.AddServiceBusQueueMessagePump(...)
                .WithServiceBusMessageHandler<OrdersMessageHandler, Order>()
                .WithMessageHandler<CustomerMessageHandler, Customer>();
    }
}
```

## Alternative Service Bus message routing
By default, when registering the Azure Service Bus message pump a built-in message router is registered to handle the routing throughout the previously registered message handlers.

This router is registered with the `IAzureServiceBusMessageRouter` interface (which implements the more general `IMessageRouter` for non-specific Service Bus messages).

When you want for some reason alter the message routing or provide additional functionality, you can register your own router which the Azure Service Bus message pump will use instead.

The following example shows you how a custom router is used for additional tracking. Note that the `AzureServiceBusMessageRouter` implements the `IAzureServiceBusMessageRouter` so we can override the necessary implementations.

```csharp
public class TrackedAzureServiceBusMessageRouter : AzureServiceBusMessageRouter
{
    public TrackedAzureServiceBusMessageRouter(IServiceProvider serviceProvider, ILogger<AzureServiceBusMessageRouter> logger)
        : base(serviceProvider, logger)
    {
    }

    public override Task ProcessMessageAsync(
        ServiceBusReceivedMessage message,
        AzureServiceBusMessageContext messageContext,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        Logger.LogTrace("Start routing incoming message...");
        base.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
        Logger.LogTrace("Done routing incoming message!");
    }
}
```

This custom message router can be registered with the following extension:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBusMessageRouting(serviceProvider =>
    {
        var logger = serviceProvider.GetService<ILogger<TrackedAzureServiceBusMessageRouter>>();
        return new TrackedAzureServiceBusMessageRouter(serviceProvider, logger);
    });

    services.AddServiceBusQueueMessagePump(...);
}
```

> Note that your own router should be registered **before** you register the Azure Message Pump otherwise it cannot be overridden.

## Message Correlation
The message correlation of the received messages is set automatically. All the message handlers will have access to the current `MessageCorrelationInfo` correlation model for the specific currently processed message.

The correlation information of Azure Service Bus messages can also be retrieved from an extension that wraps all correlation information.

```csharp
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;

ServiceBusReceivedMessage message = ...
MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

// Unique identifier that indicates an attempt to process a given message.
string cycleId = correlationInfo.CycleId;

// Unique identifier that relates different requests together.
string transactionId = correlationInfo.TransactionId;

// Unique identifier that distinguishes the request.
string operationId = correlationInfo.OperationId;
```

To retrieve the correlation information in other application code, you can use a dedicated marker interface called `IMessageCorrelationInfoAccessor`.
Note that this is a scoped dependency and so will be the same instance across a scoped operation.

```csharp
using Arcus.Messaging.Abstractions;

public class DependencyService
{
    private readonly IMessageCorrelationInfoAccessor _accessor;

    public DependencyService(IMessageCorrelationInfoAccessor accessor)
    {
        _accessor = accessor;
    }

    public void Method()
    {
        MessageCorrelationInfo correlation = _accessor.GetCorrelationInfo();

        _accessor.SetCorrelation(correlation);
    }
}
```

## Want to get started easy? Use our templates!
We provide templates to get started easily:

- Azure Service Bus Queue Worker Template ([docs](https://templates.arcus-azure.net/features/servicebus-queue-worker-template))
- Azure Service Bus Topic Worker Template ([docs](https://templates.arcus-azure.net/features/servicebus-topic-worker-template))

[&larr; back](/)
