---
title: "Azure Event Hubs message handling"
layout: default
---

# Azure Event Hubs message handling
The `Arcus.Messaging.Pumps.EventHubs` library provides ways to perform all the plumbing that is required for processing event messages on Azure EventHubs. 

As a user, the only thing you have to do is **focus on processing messages, not how to get them**. Following terms are used:
- **Message handler**: implementation that processes the received message from an Azure EventHubs. Message handlers are created by implementing the `IAzureEventHubsMessageHandler<TMessage>`. This message handler will be called upon when a message is available in the Azure EventHubs. [this section](#message-handler-example) for a message handler example setup
- **Message router**: implementation that delegates the received Azure EventHubs event message to the correct message handler.
- **Message pump**: implementation that interacts and receives the Azure EventHubs event message. The pump can be configured for different scenarios, see [this section](#pump-configuration) for more information.

![Message handling schema](/media/worker-eventhubs-message-handling.png)

## Installation
This features requires to install our NuGet package:

```shell
PM > Install-Package Arcus.Messaging.Pumps.EventHubs
```

## Message handler example
Here is an example of a message handler that expects messages of type `Order`:

```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.EventHubs;
using Microsoft.Extensions.Logging;

public class OrdersMessageHandler : IAzureEventHubsMessageHandler<Order>
{
    private readonly ILogger _logger;

    public OrdersMessageHandler(ILogger<OrdersMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        Order orderMessage, 
        AzureEventHubsMessageContext messageContext, 
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

public class Program
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Arcus secret store will be used to lookup the connection strings, 
        // for more information about the Arcus secret store see: https://security.arcus-azure.net/features/secret-store
        services.AddSecretStore(stores => ...);

        // Add Azure EventHubs message pump and use OrdersMessageHandler to process the messages.
        services.AddEventHubsMessagePump("<my-eventhubs-name>", "Arcus_EventHubs_ConnectionString", "<my-eventhubs-blob-storage-container-name>", "Arcus_EventHubs_Blob_ConnectionString")
                .WithEventHubsMessageHandler<OrdersMessageHandler, Order>();

        // Note, that only a single call to the `.WithEventHubsMessageHandler` has to be made when the handler should be used across message pumps.
    }
}
```

The Azure EventHubs uses the `EventProcessorClient` internally. To learn more about this way of consuming messages from Azure EventHubs, see [Microsoft's official documentation](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventhubs.processor-readme).

In this example, we are using the Azure EventHubs message pump to process event messages and use the connection strings stored in the Arcus secret store:
- Azure EventHubs name: The name of the Event Hub that the processor is connected to, specific to the EventHubs namespace that contains it.
- Azure EventHubs connection string secret name: The name of the secret to retrieve the Azure EventHubs connection string using your registered Arcus secret store implementation.
- Azure EventHubs Blob storage container name: The name of the Azure Blob storage container in the storage account to reference where the event checkpoints will be stored. The events will be streamed to this storage so that the client only has to worry about event processing, not event capturing.
- Azure EventHubs Blob storage account connection string secret name: The name of the secret to retrieve the Azure EventHubs connection string using your registered Arcus secret store implementation.

## Pump Configuration
The Azure EventHubs message pump can be configured further to met your needs.

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEventHubsMessagePump(..., options =>
        {
            // The name of the consumer group this processor is associated with. Events are read in the context of this group. 
            // Default: "$Default"
            options.ConsumerGroup = "<my-eventhubs-consumer-group>";
        });
    }
}
```

## Message Correlation
The message correlation of the received messages is set automatically. All the message handlers will have access to the current `MessageCorrelationInfo` correlation model for the specific currently processed message.

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