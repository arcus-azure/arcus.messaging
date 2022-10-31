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
Here is an example of a message handler that expects messages of type `SensorReading`:

```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.EventHubs;
using Microsoft.Extensions.Logging;

public class SensorReadingMessageHandler : IAzureEventHubsMessageHandler<SensorReading>
{
    private readonly ILogger _logger;

    public SensorReadingMessageHandler(ILogger<SensorReadingMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        SensorReading message, 
        AzureEventHubsMessageContext messageContext, 
        MessageCorrelationInfo correlationInfo, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing sensor reading {SensorId} for ", message.Id);

        // Process the message.

        _logger.LogInformation("Sensor reading {SensorId} processed", message.Id);
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
                .WithEventHubsMessageHandler<SensorReadingMessageHandler, SensorReading>();

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

> ⚠ The order in which the message handlers are registered matters when a message is processed. If the first one can't handle the message, the second will be checked, and so forth.

### Filter messages based on message context
When registering a new message handler, one can opt-in to add a filter on the message context which filters out messages that are not needed to be processed.

This can be useful when you are sending different message types on the same queue. Another use-case is being able to handle different versions of the same message type which have different contracts because you are migrating your application.

Following example shows how a message handler should only process a certain message when a property's in the context is present.

We'll use a simple message handler implementation:

```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessagingHandling;

public class SensorReadingMessageHandler : IAzureEventHubsMessageHandler<Order>
{
    public async Task ProcessMessageAsync(SensorReading message, AzureEventHubsMessageContext context, ...)
    {
        // Do some processing...
    }
}
```

We would like that this handler only processed the message when the context contains `Location` equals `Room`.

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEventHubsMessagePump(...)
                .WithEventHubsMessageHandler<SensorReadingMessageHandler, SensorReading>(context => context.Properties["Location"].ToString() == "Room");
    }
}
```

> Note that the order in which the message handlers are registered is important in the message processing.
> In the example, when a message handler above this one is registered that could also handle the message (same message type) then that handler may be chosen instead of the one with the specific filter.

### Bring your own deserialization
You can also choose to extend the built-in message deserialization with a custom deserializer to meet your needs. 
This allows you to easily deserialize into different message formats or reuse existing (de)serialization capabilities that you already have without altering the message router. 

You start by implementing an `IMessageBodySerializer`. The following example shows how an expected type can be transformed to something else. 
The result type (in this case `SensorReadingBatch`) will then be used to check if there is an `IAzureEventHubsMessageHandler` registered for that message type.

```csharp
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;

public class SensorReadingBatchMessageBodySerializer : IMessageBodySerializer
{
    public async Task<MessageResult> DeserializeMessageAsync(string messageBody)
    {
        var serializer = new XmlSerializer(typeof(SensorReading[]));
        using (var contents = new MemoryStream(Encoding.UTF8.GetBytes(messageBody)))
        {
            var messages = (SensorReading[]) serializer.Deserialize(contents);
            return MessageResult.Success(new SensorReadingBatch(messages));
        }
    }
}
```

The registration of these message body serializers can be done just as easily as an `IAzureEventHubsMessageHandler`:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the message body serializer in the dependency container where the dependent services will be injected.
        services.AddEventHubsMessagePump(...)
                .WithEventHubsMessageHandler<SensorReadingBatchMessageHandler, SensorReading>(..., messageBodySerializer: new OrderBatchMessageBodySerializer());

        // Register the message body serializer  in the dependency container where the dependent services are manually injected.
        services.AddEventHubsMessagePump(...)
                .WithEventHubsMessageHandler(..., messageBodySerializerImplementationFactory: serviceProvider => 
                {
                    var logger = serviceProvider.GetService<ILogger<OrderBatchMessageHandler>>();
                    return new SensorReadingBatchMessageHandler(logger);
                });
    }
}
```

> Note that the order in which the message handlers are registered is important in the message processing.
> In the example, when a message handler above this one is registered that could also handle the message (same message type) then that handler may be chosen instead of the one with the specific filter.

### Filter messages based on message body
When registering a new message handler, one can opt-in to add a filter on the incoming message body which filters out messages that are not needed to be processed by this message handler.
This can be useful when you want to route messages based on the message content itself instead of the messaging context.

Following example shows how a message handler should only process a certain message when the status is 'Active'; meaning only `SensorReading` with active sensors will be processed.

```csharp
// Message to be sent:
public enum SensorStatus { Active, Idle }

public class SensorReading
{
    public string SensorId { get; set; }
    public SensorStatus Status { get; set; }
}

using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.Abstractions.MessageHandling;

// Message handler
public class SensorReadingMessageHandler : IAzureEventHubsMessageHandler<SensorReading>
{
    public async Task ProcessMessageAsync(SensorReading message, AzureEventHubsMessageContext context, ...)
    {
        // Do some processing...
    }
}

using Microsoft.Extensions.DependencyInjection;

// Message handler registration
public class Program
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEventHubsMessagePump(...)
                .WithEventHubsMessageHandler<SensorReadingMessageHandler, SensorReading>((SensorReading sensor) => sensor.Status == SensorStatus.Active);
    }
}
```

### Fallback message handling
When receiving a message on the message pump and none of the registered `IAzureEventHubsMessageHandler`'s can correctly process the message, the message pump normally throws and logs an exception.
It could also happen in a scenario that's to be expected that some received messages will not be processed correctly (or you don't want them to).

In such a scenario, you can choose to register a `IFallbackMessageHandler` in the dependency container. 
This extra message handler will then process the remaining messages that can't be processed by the normal message handlers.

Following example shows how such a message handler can be implemented:

```csharp
using Arcus.Messaging.Pumps.EventHubs;
using Microsoft.Extensions.Logging;

public class WarnsUserFallbackMessageHandler : IFallbackMessageHandler
{
    private readonly ILogger _logger;

    public WarnsUserFallbackMessageHandler(ILogger<WarnsUserFallbackMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(string message, MessageContext context, ...)
    {
        _logger.LogWarning("These type of messages are expected not to be processed");
    }
}
```

And to register such an implementation:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEventHubsMessagePump(...)
                .WithFallbackMessageHandler<WarnsUserFallbackMessageHandler>();
    }
}
```

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

            // The name of the Azure EventHubs message property that has the transaction ID.
            // (default: Transaction-Id).
            options.Routing.Correlation.TransactionIdPropertyName = "X-Transaction-ID";

            // The format of the message correlation used when receiving Azure EventHubs messages.
            // (default: W3C).
            options.Correlation.Format = MessageCorrelationFormat.Hierarchical;

            // The name of the Azure EventHubs message property that has the upstream service ID.
            // ⚠ Only used when the correlation format is configured as Hierarchical.
            // (default: Operation-Parent-Id).
            options.Routing.Correlation.OperationParentIdPropertyName = "X-Operation-Parent-ID";

            // The property name to enrich the log event with the correlation information cycle ID.
            // ⚠ Only used when the correlation format is configured as Hierarchical.
            // (default: CycleId)
            options.Routing.CorrelationEnricher.CycleIdPropertyName = "X-CycleId";

            // Indicate whether or not the default built-in JSON deserialization should ignore additional members 
            // when deserializing the incoming message (default: AdditionalMemberHandling.Error).
            options.Routing.Deserialization.AdditionalMembers = AdditionalMembersHandling.Ignore;
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