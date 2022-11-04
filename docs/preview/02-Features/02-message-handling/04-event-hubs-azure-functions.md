---
title: "Azure EventHubs message handling for Azure Functions"
layout: default
---

This article describes how you can use Arcus' message handler concept with Azure Functions; allowing you to more easily port your business logic from/to Azure Functions.

# Azure EventHubs message handling for Azure Functions
While our default message pump system provides a way to receive, route, and handle incoming EventHubs messages which are, unfortunately, not supported in Azure Functions.
Today, Azure Functions acts as a message receiver meaning that the function is triggered when a message is available but does not handle message routing and handling. However, in this case, it acts as the message pump.

Following terms are used:
- **Message handler**: implementation that processes the received message from an Azure EventHubs subscription. Message handlers are created by implementing the `IAzureEventHubsMessageHandler<TMessage>`. This message handler will be called upon when a message is available on the Azure EventHubs subscription.
- **Message router**: implementation that delegates the received Azure EventHubs message to the correct message handler.

That's why we extracted our message routing functionality so you can call it directly from your Azure Function.

![Azure Functions message handling](/media/az-func-eventhubs-message-handling.png)

We will walk you through the process of using message handlers with Azure Functions:

## Installation
To use the following described features, install the following package:
```shell
PM > Install-Package -Name Arcus.Messaging.AzureFunctions.EventHubs
```

## Receive Azure EventHubs message in an Azure Function
Here's an example of how an Azure Function receives an Azure EventHubs message from a topic:

```csharp
public class SensorReadingFunction
{
    [Function("sensor-reading")]
    public async Task Run(
        [EventHubTrigger("sensors", Connection = "EventHubsConnectionString")] string[] messages,
        Dictionary<string, JsonElement>[] propertiesArray,
        FunctionContext context)
    {
        // Processing events...
    }
}
```

## Declaring our Azure EventHubs message handlers
Registering message handlers to process the EventHubs message happens just the same as using a message pump.
Here is an example of two message handlers that are being registered during startup:

Processing sensor reading updates:

```csharp
public class SensorReadingUpdateEventHubsMessageHandler : IAzureEventHubsMessageHandler<SensorReadingUpdate>
{
    private readonly ILogger _logger;

    public SensorReadingUpdateEventHubsMessageHandler(ILogger<SensorReadingUpdateEventHubsMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        SensorReadingUpdate readingUpdate,
        AzureEventHubsMessageContext messageContext,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing sensor reading {SensorId} in room {Room}", readingUpdate.SensorId, readingUpdate.RoomName);
    }
}
```

Processing sensor config updates:

```csharp
public class SensorConfigUpdateMessageHandler : IAzureEventHubsMessageHandler<SensorConfigUpdate>
{
    private readonly ILogger _logger;

    public SensorConfigUpdateMessageHandler(ILogger<SensorConfigUpdateMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        SensorConfigUpdate configUpdate,
        AzureEventHubsMessageContext messageContext,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        log.LogInformation("Processing sensor config {SensorId} in room {Room}", configUpdate.SensorId, configUpdate.Room);
    }
}
```

Now that we have created our message handlers, we can declare when we should use them by registering them with our router.

## Processing received messages through the message router
Now that everything is setup, we need to actually use the declared message handlers by routing the messages from the Azure Function into the correct message handler.

To achieve that, we need to add message routing with the `.AddEventHubsMessageRouting` extension:

### Isolated Azure Funcions
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.Services.AddEventHubsMessageRouting()
                        .WithEventHubsMessageHandler<SensorReadingUpdateMessageHandler, SensorReadingUpdate>()
                        .WithEventHubsMessageHandler<SensorConfigUpdateMessageHandler, SensorConfigUpdate>();
    })
    .Build();

host.Run();
```

### In-Process Azure Functions
```csharp
[assembly: FunctionsStartup(typeof(Startup))]
namespace SensorReading
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddEventHubsMessageRouting()
                            .WithEventHubsMessageHandler<SensorReadingUpdateMessageHandler, SensorReadingUpdate>()
                            .WithEventHubsMessageHandler<SensorConfigUpdateMessageHandler, SensorConfigUpdate>();
        }
    }
}
```

This extension will register an `IAzureEventHubsMessageRouter` interface that allows you to interact with the registered message handlers in a easy manner.

> ⚡ It also registers an more general `IMessageRouter` you can use if the general message routing (with the message raw message body as `string` as incoming message) will suffice.

We can now inject the message router in our Azure Function and process all messages with it.
This will determine what the matching message handler is and process it accordingly:

### Isolated
```csharp
using Arcus.Messaging.Abstractions.EventHubs;
using Azure.Messaging.EventHubs;

public class SensorReadingFunction
{
    private readonly IAzureEventHubsMessageRouter _messageRouter;

    public SensorReadingFunction(IAzureEventHubsMessageRouter messageRouter)
    {
        _messageRouter = messageRouter;
    }

    [Function("sensor-reading")]
    public async Task Run(
        [EventHubTrigger("sensor-reading", Connection = "EventHubsConnectionString")] string[] messages,
        Dictionary<string, JsonElement>[] propertiesArray,
        FunctionContext executionContext)
    {
        _logger.LogInformation("First EventHubs triggered message: {Message}", messages[0]);
    
        for (var i = 0; i < messages.Length; i++)
        {
            string eventBody = messages[i];
            Dictionary<string, JsonElement> eventProperties = propertiesArray[i];
            
            EventData eventData = CreateEventData(message, eventProperties);
            AzureEventHubsMessageContext messageContext = eventData.GetMessageContext("<namespace>", "$Default", "<eventhubs-name>");
    
            using (MessageCorrelationResult result = executionContext.GetCorrelationInfo(eventProperties))
            {
                await _messageRouter.RouteMessageAsync(eventData, messageContext, result.CorrelationInfo, CancellationToken.None);
            }
        }
    }
    
    private static EventData CreateEventData(string eventBody, IDictionary<string, JsonElement> properties)
    {
        var data = new EventData(eventBody);
        foreach (KeyValuePair<string, JsonElement> property in properties)
        {
            data.Properties.Add(property.Key, property.Value.ToString());
        }
    
        return data;
    }
}
```

### In-Process
```csharp
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Abstractions.EventHubs;
using Arcus.Messaging.Abstractions.EventHubs.MessageHandling;
using Arcus.Messaging.AzureFunctions.EventHubs;

public class SensorReadingFunction
{
    private readonly IAzureEventHubsMessageRouter _messageRouter;
    private readonly AzureFunctionsInProcessMessageCorrelation _messageCorrelation;

    public SensorReadingFunction(
        IAzureEventHubsMessageRouter messageRouter,
        AzureFunctionsInProcessMessageCorrelation messageCorrelation)
    {
        _messageRouter = messageRouter;
        _messageCorrelation = messageCorrelation;
    }

    [FunctionName("sensor-reading")]
    public async Task Run(
        [EventHubTrigger("sensors", Connection = "EventHubsConnectionString")] EventData[] events,
        ILogger log,
        CancellationToken cancellationToken)
    {
         foreach (EventData message in events)
         {
             log.LogInformation($"First Event Hubs triggered message: {message.MessageId}");

             var messageContext = message.GetMessageContext("sensor-reading.servicebus.windows.net", "$Default", "sensors");
             
              // W3C message correlation (with built-in Microsoft dependency tracking, recommended).
             using (MessageCorrelationResult result = _messageCorrelation.CorrelateMessage(message))
             {
                 await _messageRouter.RouteMessageAsync(message, messageContext, result.CorrelationInfo, cancellationToken);
             }

             // Hierarchical message correlation (without built-in Microsoft dependency tracking).
             MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();
             await _messageRouter.RouteMessageAsync(message, messageContext, correlationInfo, cancellationToken);
         }
    }
}
```

Upon receival of an Azure EventHubs message, the message will be either routed to one of the two previously registered message handlers.