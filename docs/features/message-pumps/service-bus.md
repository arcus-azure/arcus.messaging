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

You can do this by creating a message handler which derives from `AzureServiceBusMessagePump<TMessage>`

Here is an example of a message handler that expects messages of type `Order`:

```csharp
public class OrdersMessageHandler : AzureServiceBusMessagePump<Order>
{
    public OrdersMessageHandler(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<OrdersMessageHandler> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(Order orderMessage, AzureServiceBusMessageContext messageContext, MessageCorrelationInfo correlationInfo, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber, orderMessage.Customer.FirstName, orderMessage.Customer.LastName);

        // Custom logic

        Logger.LogInformation("Order {OrderId} processed", orderMessage.Id);
    }
}
```

As of today, the message handler is tightly coupled to the broker but overtime they will be decoupled.

Over time you'll be able to:
- Bring your own deserialization
- Re-use a message handler across different brokers
- Use multiple message handlers and let the pump route the messages to the correct handler.
This can be based on message type, message context or custom message flow determination

## Configuration

Once the message handler is created, you can very easily configure it:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add Service Bus Queue message pump and use OrdersMessageHandler to process the messages
    // ISecretProvider will be used to lookup the connection string scoped to the queue for secret ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING
    services.AddServiceBusQueueMessagePump<OrdersMessageHandler>("ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING");

    // Add Service Bus Topic message pump and use OrdersMessageHandler to process the messages on the 'My-Subscription-Prefix' prefix subscription
    // ISecretProvider will be used to lookup the connection string scoped to the queue for secret ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING
    services.AddServiceBusTopicMessagePump<OrdersMessageHandler>("My-Subscription-Prefix", "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING");
}
```

In this example, we are using the Azure Service Bus message pump to process a queue and a topic and use the connection string stored in the `ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING` connection string.

We support **connection strings that are scoped on the Service Bus namespace and entity** allowing you to choose the required security model for your applications. If you are using namespace-scoped connection strings you'll have to pass your queue/topic name as well.

### Customized Configuration

Next to that, we provide a **variety of overloads** to allow you to:

- Specify the name of the queue/topic
- Configure how the message pump should work *(ie. max concurrent calls & auto delete)*
- Read the connection string from the configuration *(although we highly recommend using a secret store instead)*

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Specify the name of the Service Bus Queue:
    services.AddServiceBusQueuePump<OrdersMessageHandler>(
        "My-Service-Bus-Queue-Name",
        "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING");

    // Specify the name of the Service Bus Topic, and provide a prefix for the Topic subscription:
    services.AddServiceBusTopicPump<OrdersMessageHandler>(
        "My-Service-Bus-Topic-Name",
        "My-Service-Bus-Topic-Subscription-Prefix",
        "ARCUS_SERVICEBUS_ORDERS_CONNECTIONSTRING");

    services.AddServiceBusQueuePump<OrdersMessageHandler>(
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
            // when the message pump starts/stops (default: DeleteOnStop, which implicitly means also 'create on start').
            options.TopicSubscription = TopicSubscription.CreateOnStart | TopicSubscription.DeleteOnStop;
        });
}
```

## Want to get started easy? Use our templates!

We provide templates to get started easily:

- Azure Service Bus Queue Worker Template ([docs](https://templates.arcus-azure.net/features/servicebus-queue-worker-template))
- Azure Service Bus Topic Worker Template ([docs](https://templates.arcus-azure.net/features/servicebus-topic-worker-template))
