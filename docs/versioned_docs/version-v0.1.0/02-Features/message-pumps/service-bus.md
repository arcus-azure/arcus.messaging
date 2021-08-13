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
using Arcus.Messaging.Pumps.Abstractions.MessageHanlding;

public class OrdersMessageHandler : IMessageHandler<Order>
{
    public async Task ProcessMessageAsync(
        Order orderMessage, 
        MessageContext messageContext, 
        MessageCorrelationInfo correlationInfo, 
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber, orderMessage.Customer.FirstName, orderMessage.Customer.LastName);

        // Custom logic

        Logger.LogInformation("Order {OrderId} processed", orderMessage.Id);
    }
}
```

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
                // if no exceptions occured andprocessing has finished (default: true).
                options.AutoComplete = true;

                // The amount of concurrent calls to process messages 
                // (default: null, leading to the defaults of the Azure Service Bus SDK message handler options).
                options.MaxConcurrentCalls = 5;

                // The unique identifier for this background job to distinguish 
                // this job instance in a multi-instance deployment (default: guid).
                options.JobId = Guid.NewGuid().ToString();

                // Indicate whether or not a new Azure Service Bus Topic subscription should be created/deleted
                // when the message pump starts/stops (default: CreateOnStart & DeleteOnStop).
                options.TopicSubscription = TopicSubscription.CreateOnStart | TopicSubscription.DeleteOnStop;
            });

        services.AddServiceBusQueueMessagePump(
            "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING",
            options => 
            {
                // Indicate whether or not messages should be automatically marked as completed 
                // if no exceptions occured andprocessing has finished (default: true).
                options.AutoComplete = true;

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

// Unique idenfier that distinguishes the request.
string operationId = correlationInfo.OperationId;
```

## Want to get started easy? Use our templates!

We provide templates to get started easily:

- Azure Service Bus Queue Worker Template ([docs](https://templates.arcus-azure.net/features/servicebus-queue-worker-template))
- Azure Service Bus Topic Worker Template ([docs](https://templates.arcus-azure.net/features/servicebus-topic-worker-template))

[&larr; back](/)