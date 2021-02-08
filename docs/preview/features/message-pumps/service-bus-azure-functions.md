---
title: "Azure Service Bus Message Pump for Azure Functions"
layout: default
---

# Azure Service Bus Message Pump for Azure Functions
While our default message pump system provides a way to receive, route, and handle incoming Service Bus messages which are, unfortunately, not supported in Azure Functions.
Today, Azure Functions acts as a message receiver meaning that the function is triggered when a message is available but does not handle message routing and handling. However, in this case, it acts as the message pump.

That's why we extracted our message routing functionality so you can call it directly from your Azure Function.

- [Receive Azure Service Bus message in an Azure Function](#receive-azure-service-bus-message-in-an-azure-function)
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
Here's two message handlers with their registration in the `Startup.cs` of this example:

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

Registering both message handlers:

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
To actually use these registered message handles in the Azure Function, you'll have to register the Azure Service Bus message router.

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
