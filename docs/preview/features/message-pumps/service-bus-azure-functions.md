---
title: "Azure Service Bus Message Pump for Azure Functions"
layout: default
---

This article describes how you can use Arcus' message handler concept with Azure Functions; allowing you to more easily port your business logic from/to Azure Functions.

# Azure Service Bus Message Pump for Azure Functions
While our default message pump system provides a way to receive, route, and handle incoming Service Bus messages which are, unfortunately, not supported in Azure Functions.
Today, Azure Functions acts as a message receiver meaning that the function is triggered when a message is available but does not handle message routing and handling. However, in this case, it acts as the message pump.

That's why we extracted our message routing functionality so you can call it directly from your Azure Function.

We will walk you through the process of using message handlers with Azure Functions:

1. [Create a new Azure Function with Service Bus binding](#receive-azure-service-bus-message-in-an-azure-function)
2. [Declaring our Azure Service Bus message handlers](#specifying-service-bus-message-handlers)
3. [Enabling message routing](#using-message-routing)
4. Processing received messages through the message router

## Receive Azure Service Bus message in an Azure Function
Here's an example of how an Azure Function receives an Azure Service Bus message from a topic:

```csharp
public class MessageProcessingFunction
{
    [FunctionName("message-processor")]
    public void Run(
        [ServiceBusTrigger("%TopicName%", "%SubscriptionName%", Connection = "ServiceBusConnectionString")] Message message, 
        ILogger log)
    {
        // Processing message...
    }
}
```

## Determine Service Bus message handlers
Registering message handlers to process the Service Bus message happens just the same as using a message pump.
Here is an example of two message handlers that are being registered during startup:

Processing shipment messages:

```csharp
public class ShipmentServiceBusMessageHandler : IAzureServiceBusMessageHandler<Shipment>
{
    private readonly ILogger _logger;

    public ShipmentServiceBusMessageHandler(ILogger<ShipmentServiceBusMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        Shipment shipment,
        AzureServiceBusMessageContext messageContext,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing shipment {ShipmentId} for order #{OrderId}", shipment.Id, shipment.Order.Id);
    }
}
```

Processing order messages:

```csharp
public class OrderServiceBusMessageHandler : IAzureServiceBusMessageHandler<Order>
{
    private readonly ILogger _logger;

    public OrderServiceBusMessageHandler(ILogger<OrderServiceBusMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        Order order,
        AzureServiceBusMessageContext messageContext,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        log.LogInformation("Processing order {OrderId} for {OrderAmount} units of {OrderArticle} bought by {CustomerFirstName} {CustomerLastName}", order.Id, order.Amount, order.ArticleNumber, order.Customer.
    }
}
```

Now that we have created our message handlers, we can declare when we should use them:

```csharp
[assembly: FunctionsStartup(typeof(Startup))]
namespace MessageProcessing
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.WithServiceBusMessageHandler<OrderServiceBusMessageHandler, Order>(messageContext => messageContext.UserProperties["MessageType"] == MessageType.Order)
                            .WithServiceBusMessageHandler<ShipmentServiceBusMessageHandler, Shipment>(messageContext => messageContext.UserProperties["MessageType"] == MessageType.Shipment);
        }
    }
}
```

## Activating message routing
Now that everything is setup, we need to actually use the declared message handlers by routing the messages from the Azure Function into the correct message handler.

To achieve that, we need to add message routing with the `.AddServiceBusMessageRouting` extension:

```csharp
[assembly: FunctionsStartup(typeof(Startup))]
namespace MessageProcessing
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddServiceBusMessageRouting()
                            .WithServiceBusMessageHandler<OrderServiceBusMessageHandler, Order>(messageContext => messageContext.UserProperties["MessageType"] == MessageType.Order)
                            .WithServiceBusMessageHandler<ShipmentServiceBusMessageHandler, Shipment>(messageContext => messageContext.UserProperties["MessageType"] == MessageType.Shipment);
        }
    }
}
```

This extension will register an `IAzureServiceBusMessageRouter` interface allows you access to message handling with specific Service Bus operations during the message processing (like dead lettering and abandonning).

It also registers an more general `IMessageRouter` you can use if the general message routing (with the message raw message body as `string` as incoming message) will suffice.

In any case, this router can be injected in your function:

```csharp
public class MessageProcessingFunction
{
    private readonly IAzureServiceBusMessageRouter _messageRouter;

    public MessageProcessingFunction(IAzureServiceBusMessageRouter messageRouter)
    {
        _messageRouter = messageRouter;
    }

    [FunctionName("message-processor")]
    public void Run(
        [ServiceBusTrigger("%TopicName%", "%SubscriptionName%", Connection = "ServiceBusConnectionString")] Message message, 
        ILogger log,
        CancellationToken cancellationToken)
    {
        var messageContext = new AzureServiceBusMessageContext(message.MessageId, message.SystemProperties, message.UserProperties);
        MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

        _messageRouter.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
    }
}
```

Upon receival of an Azure Service Bus message, the message will be either routed to one of the two previously registered message handlers.

[&larr; back](/)
