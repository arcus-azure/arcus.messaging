---
title: "Azure Service Bus message handling for Azure Functions"
layout: default
---

This article describes how you can use Arcus' message handler concept with Azure Functions; allowing you to more easily port your business logic from/to Azure Functions.

# Azure Service Bus message handling for Azure Functions
While our default message pump system provides a way to receive, route, and handle incoming Service Bus messages which are, unfortunately, not supported in Azure Functions.
Today, Azure Functions acts as a message receiver meaning that the function is triggered when a message is available but does not handle message routing and handling. However, in this case, it acts as the message pump.

Following terms are used:
- **Message handler**: implementation that processes the received message from an Azure Service Bus queue or topic subscription. Message handlers are created by implementing the `IAzureServiceBusMessageHandler<TMessage>`. This message handler will be called upon when a message is available in the Azure Service Bus queue or on the topic subscription.
- **Message router**: implementation that delegates the received Azure Service Bus message to the correct message handler.

That's why we extracted our message routing functionality so you can call it directly from your Azure Function.

![Azure Functions message handling](/media/az-func-message-handling.png)

We will walk you through the process of using message handlers with Azure Functions:

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

## Declaring our Azure Service Bus message handlers
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

Now that we have created our message handlers, we can declare when we should use them by registering them with our router.

## Processing received messages through the message router
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
            builder.AddServiceBusMessageRouting()
                   .WithServiceBusMessageHandler<OrderV1ServiceBusMessageHandler, OrderV1>(messageContext => messageContext.UserProperties["MessageType"] == MessageType.OrderV1)
                   .WithServiceBusMessageHandler<OrderV2ServiceBusMessageHandler, OrderV2>(messageContext => messageContext.UserProperties["MessageType"] == MessageType.OrderV2);
        }
    }
}
```

This extension will register an `IAzureServiceBusMessageRouter` interface allows you access to message handling with specific Service Bus operations during the message processing (like dead lettering and abandoning).

It also registers an more general `IMessageRouter` you can use if the general message routing (with the message raw message body as `string` as incoming message) will suffice.

We can now inject the message router in our Azure Function and process all messages with it.
This will determine what the matching message handler is and process it accordingly.
Upon receival of an Azure Service Bus message, the message will be either routed to one of the two previously registered message handlers.

### Isolated Azure Functions
```csharp
using Arcus.Messaging.Abstractions.ServiceBus;
using Arcus.Messaging.AzureFunctions.ServiceBus;
using Azure.Messaging.ServiceBus;

public class MessageProcessingFunction
{
    private readonly IAzureServiceBusMessageRouter _messageRouter;
    private readonly string _jobId;

    public MessageProcessingFunction(IAzureServiceBusMessageRouter messageRouter)
    {
        _jobId = $"job-{Guid.NewGuid()}";
        _messageRouter = messageRouter;
    }

    [Function("message-processor")]
    public void Run(
        [ServiceBusTrigger("%TopicName%", "%SubscriptionName%", Connection = "ServiceBusConnectionString")] byte[] messageBody, 
        FunctionContext executionContext)
    {
        ServiceBusReceivedMessage message = ConvertToServiceBusMessage(messageBody, executionContext);

        AzureServiceBusMessageContext messageContext = message.GetMessageContext(_jobId);
        using (MessageCorrelationResult result = executionContext.GetCorrelationInfo())
        {
            _messageRouter.ProcessMessageAsync(message, messageContext, result.CorrelationInfo, cancellationToken);
        }
    }

     private static ServiceBusReceivedMessage ConvertToServiceBusMessage(byte[] messageBody, FunctionContext context)
     {
         var applicationProperties = new Dictionary<string, object>();
         if (context.BindingContext.BindingData.TryGetValue("ApplicationProperties", out object applicationPropertiesObj))
         {
             var json = applicationPropertiesObj.ToString();
             applicationProperties = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
         }

         var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
             body: BinaryData.FromBytes(messageBody),
             messageId: context.BindingContext.BindingData["MessageId"]?.ToString(),
             correlationId: context.BindingContext.BindingData["CorrelationId"]?.ToString(),
             properties: applicationProperties);

         return message;
     }
}
```

### In-Process Azure Functions
```csharp
using Arcus.Messaging.Abstractions.ServiceBus;
using Azure.Messaging.ServiceBus;

public class MessageProcessingFunction
{
    private readonly IAzureServiceBusMessageRouter _messageRouter;
    private readonly string _jobId;

    public MessageProcessingFunction(IAzureServiceBusMessageRouter messageRouter)
    {
        _jobId = $"job-{Guid.NewGuid()}";
        _messageRouter = messageRouter;
    }

    [FunctionName("message-processor")]
    public void Run(
        [ServiceBusTrigger("%TopicName%", "%SubscriptionName%", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message, 
        ILogger log,
        CancellationToken cancellationToken)
    {
        // W3C message correlation (with automatic tracking of built-in Microsoft dependencies)
        AzureServiceBusMessageContext messageContext = message.GetMessageContext(_jobId);
        using (var result = AzureFunctionsMessageCorrelation.CorrelateMessage(message))
        {
            _messageRouter.ProcessMessageAsync(message, messageContext, result.CorrelationInfo, cancellationToken);
        }

        // Hierarchical message correlation (without automatic tracking of built-in Microsoft dependencies))
        AzureServiceBusMessageContext messageContext = message.GetMessageContext(_jobId);
        MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

        _messageRouter.ProcessMessageAsync(message, messageContext, correlationInfo, cancellationToken);
    }
}
```