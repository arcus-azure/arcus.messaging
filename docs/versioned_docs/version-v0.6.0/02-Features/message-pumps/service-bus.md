---
title: "Azure Service Bus Message Pump"
layout: default
---

# Azure Service Bus Message Pump

Azure Service Bus Message Pump will perform all the plumbing that is required for processing queues & topics:

- Manage message pump lifecycle
- Deserialize messages into concrete types
- Interpret message to provide correlation & context information
- Provide exception handling
- Provide telemetry

As a user, the only thing you have to do is **focus on processing messages, not how to get them**.
You can do this by creating a message handler which implements from `IAzureServiceBusMessageHandler<TMessage>` (or `IMessageHandler<TMessage, MessageContext>`).

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

        // Custom logic

        _logger.LogInformation("Order {OrderId} processed", orderMessage.Id);
    }
}
```

or with using the more general `IMessageHandler<>`, that will use the more general `MessageContext` instead of the one specific for Azure Service Bus.

```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;
using Microsoft.Extensions.Logging;

public class OrdersMessageHandler : IMessageHandler<Order>
{
    private readonly ILogger _logger;

    public OrdersMessageHandler(ILogger<OrdersMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        Order orderMessage, 
        MessageContext messageContext, 
        MessageCorrelationInfo correlationInfo, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber, orderMessage.Customer.FirstName, orderMessage.Customer.LastName);

        // Custom logic

        _logger.LogInformation("Order {OrderId} processed", orderMessage.Id);
    }
}
```

Other topics:
- [Azure Service Bus Message Pump](#azure-service-bus-message-pump)
  - [Configuration](#configuration)
    - [Customized Configuration](#customized-configuration)
  - [Fallback message handling](#fallback-message-handling)
  - [Influence handling of Service Bus message in message handler](#influence-handling-of-service-bus-message-in-message-handler)
    - [During (regular) message handling](#during-regular-message-handling)
    - [During fallback message handling](#during-fallback-message-handling)
  - [Correlation](#correlation)
  - [Automatic Azure Key Vault credentials rotation](#automatic-azure-key-vault-credentials-rotation)
  - [How does this work?](#how-does-this-work)
    - [Installation](#installation)
    - [Usage](#usage)
  - [Want to get started easy? Use our templates!](#want-to-get-started-easy-use-our-templates)

## Configuration

Once the message handler is created, you can very easily configure it:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add Service Bus Queue message pump and use OrdersMessageHandler to process the messages
        // ISecretProvider will be used to lookup the connection string scoped to the queue for secret ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING
        services.AddServiceBusQueueMessagePump("ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING")
                .WithServiceBusMessageHandler<OrdersMessageHandler, Order>();

        // Add Service Bus Topic message pump and use OrdersMessageHandler to process the messages on the 'My-Subscription-Name' subscription
        // ISecretProvider will be used to lookup the connection string scoped to the queue for secret ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING
        services.AddServiceBusTopicMessagePump("My-Subscription-Name", "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING")
                .WithServiceBusMessageHandler<OrdersMessageHandler, Order>();

        // Note, that only a single call to the `.WithServiceBusMessageHandler` has to be made when the handler should be used across message pumps.
    }
}
```

In this example, we are using the Azure Service Bus message pump to process a queue and a topic and use the connection string stored in the `ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING` connection string.

We support **connection strings that are scoped on the Service Bus namespace and entity** allowing you to choose the required security model for your applications. If you are using namespace-scoped connection strings you'll have to pass your queue/topic name as well.

### Customized Configuration

Next to that, we provide a **variety of overloads** to allow you to:

- Specify the name of the queue/topic
- Only provide a prefix for the topic subscription, so each topic message pump is handling messages on separate subscriptions
- Configure how the message pump should work *(ie. max concurrent calls & auto delete)*
- Read the connection string from the configuration *(although we highly recommend using a secret store instead)*

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
        services.AddServiceBusMessageTopicPump<OrdersMessageHandler>(
            "My-Service-Bus-Topic-Name",
            "My-Service-Bus-Topic-Subscription-Name",
            "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING");

        // Specify a topic subscription prefix instead of a name to separate topic message pumps.
        services.AddServiceBusTopicPumpWithPrefix(
            "My-Service-Bus-Topic-Name"
            "My-Service-Bus-Subscription-Prefix",
            "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING");

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
            });

        // Multiple message handlers can be added to the servies, based on the message type (ex. 'Order', 'Customer'...), 
        // the correct message handler will be selected.
        services.WithServiceBusMessageHandler<OrdersMessageHandler, Order>()
                .WithMessageHandler<CustomerMessageHandler, Customer>();
    }
}
```

## Fallback message handling

When receiving a message on the message pump and none of the registered `IAzureServiceBusMessageHandler`'s can correctly process the message, the message pump normally throws and logs an exception.
It could also happen in a scenario that's to be expected that some received messages will not be processed correctly (or you don't want them to).

In such a scenario, you can choose to register a `IAzureServiceBusFallbackMessageHandler` in the dependency container. 
This extra message handler will then process the remaining messages that can't be processed by the normal message handlers.

Following example shows how such a message handler can be implemented:

```csharp
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Messaging.Pumps.ServiceBus.MessageHandling;
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

> Note that you have access to the Azure Service Bus message and the specific message context. These can be used to eventually call `.Abandon()` on the message.

And to register such an implementation:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.WithServiceBusFallbackMessageHandler<WarnsUserFallbackMessageHandler>();
    }
}
```

## Influence handling of Service Bus message in message handler

When an Azure Service Bus message is received (either via regular message handlers or fallback message handlers), we allow specific Azure Service Bus operations during the message handling.
Currently we support [**Dead letter**](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dead-letter-queues) and [*Abandon**](https://docs.microsoft.com/en-us/dotnet/api/microsoft.servicebus.messaging.messagereceiver.abandon?view=azure-dotnet).

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
using Arcus.Messaging.Pumps.ServiceBus.MessageHandling;
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
        services.WithServiceBusMessageHandler<AbandonUnknownOrderMessageHandler, Order>();
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
using Arcus.Messaging.Pumps.ServiceBus.MessageHandling;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

public class DeadLetterFallbackMessageHandler : AzureServiceBusFallbackMessageHandler
{
    public DeadLetterFallbackMessageHandler(ILogger<DeadLetterFallbackMessageHandler> logger)
        : base(logger)
    {
    }

    public override async Task ProcessMessageAsync(Message message, AzureServiceBusMessageContext context, ...)
    {
        Logger.LogInformation("Message is not handled by any message handler, will dead letter");
        await DeadLetterAsync(message);
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
        services.WithServiceBusFallbackMessageHandler<DeadLetterFallbackMessageHandler>();
    }
}
```

## Correlation

To retrieve the correlation information of Azure Service Bus messages, we provide an extension that wraps all correlation information.

```csharp
using Arcus.Messaging.Abstractions;
using Microsoft.Azure.ServiceBus;

Message message = ...
MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

// Unique identifier that indicates an attempt to process a given message.
string cycleId = correlationInfo.CycleId;

// Unique identifier that relates different requests together.
string transactionId = correlationInfo.TransactionId;

// Unique identifier that distinguishes the request.
string operationId = correlationInfo.OperationId;
```

## Automatic Azure Key Vault credentials rotation

The additional library `Arcus.Messaging.Pumps.ServiceBus.KeyRotation` provides an extension on the message pump to restart the pump automatically when the credentials of the pump stored in Azure Key Vault are changed.
This feature allows more reliable restarting instead of relying on authentication exceptions that may be thrown during the lifetime of the message pump.

## How does this work?

A background job is polling for `SecretNewVersionCreated` events on an Azure Service Bus Topic for the secret that stores the connection string.

That way, when the background job receives a new Key Vault event, it will get the latest connection string, restart the message pump and authenticate with the latest credentials.

### Installation

This features requires to install our NuGet package:

```shell
PM > Install-Package Arcus.Messaging.Pumps.ServiceBus.KeyRotation
```

### Usage

When the package is installed, you'll be able to use the extension in your application:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // You should have a unique Job ID to identity the message pump so the automatic process knows which pump to restart.
    string jobId = Guid.NewGuid().ToString();

    string secretName = hostContext.Configuration["ARCUS_KEYVAULT_CONNECTIONSTRINGSECRETNAME"];
    services.AddServiceBusQueueMessagePump(secretName, options => options.JobId = jobId)
            
            // This extension will be available to you once you installed the package.
            .WithAutoRestartServiceBusMessagePumpOnRotatedCredentials(
                jobId: jobId, 
                subscriptionNamePrefix: "TestSub", 
                
                // The secret key where the Azure Service Bus Topic connection string is located that the background job will use to receive the Azure Key vault events.
                serviceBusTopicConnectionStringSecretKey: "ARCUS_KEYVAULT_SECRETNEWVERSIONCREATED_CONNECTIONSTRING",
                
                // The secret key where the Azure Service Bus connection string is located that your target message pump uses.
                // This secret key name will be used to check if the received Azure Key Vault event is from this secret or not.
                messagePumpConnectionStringKey: secretName,

                // The maximum amount of thrown unauthorized exceptions that your message pump should allow before it should restart either way.
                // This amount can be used to either wait for an Azure Key Vault event or rely on the thrown unauthorized exceptions.
                maximumUnauthorizedExceptionsBeforeRestart: 5)
            
            .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
}
```

## Want to get started easy? Use our templates!

We provide templates to get started easily:

- Azure Service Bus Queue Worker Template ([docs](https://templates.arcus-azure.net/features/servicebus-queue-worker-template))
- Azure Service Bus Topic Worker Template ([docs](https://templates.arcus-azure.net/features/servicebus-topic-worker-template))

[&larr; back](/)