﻿---
title: "Azure EventHubs Extensions"
layout: default
---

# Azure EventHubs Extensions

Besides the Azure EventHubs message handling functionality, we provide several additional features related to message creation/sending and message/context discoverability.
They help in hte send/receive process of Azure EventHubs event messages.

## Installation

These features require to install our NuGet package:

```shell
PM > Install-Package Arcus.Messaging.EventHubs.Core
```

## Using Arcus secret store when registering the EventHubs producer client

When registering an `EventHubsProducerClient` via [Azure's client registration process](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventhubs-readme), the library provides an extension to pass-in a secret name instead of directly passing the Azure EventHubs connection string.
This secret name will correspond with a registered secret in the [Arcus secret store](https://security.arcus-azure.net/features/secret-store) that holds the Azure EventHubs connection string.

> ⚠ An Azure EventHubs connection string can either contain the `EntityPath` or not if it was copied from the EventHubs namespace or from the EventHub itself. In either case, make sure that you either pass in the EventHub name separately, or that the connection string contains this name. For more information, see: [How to get an Event Hubs connection string](https://docs.microsoft.com/azure/event-hubs/event-hubs-get-connection-string).

Following example shows how the secret name is passed to this extension overload:

```csharp
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Adding Arcus secret store, more info: https://security.arcus-azure.net/features/secret-store
        services.AddSecretStore(stores => stores.AddAzureKeyVaultWithManagedIdentity("https://my.vault.azure.net");

        // Adding EventHubs producer client with secret in Arcus secret store,
        // using connection string that contains EventHubs name.
        services.AddAzureClients(clients => clients.AddEventHubProducerClient(connectionStringSecretName: "<your-secret-name>"));

        // Adding EventHubs producer client with secret in Arcus secret store,
        // using connection string that does not contain EventHubs name.
        services.AddAzureClients(clients => clients.AddEventHubProducerClient(connectionStringSecretName: "<your-secret-name>", "<eventhubs-name>"));
    }
}
```

🥇 Adding your Azure EventHubs producer client this way helps separating application configuration from sensitive secrets. For more information on the added-values of the Arcus secret store, see [our dedicated documentation page](https://security.arcus-azure.net/features/secret-store).

## Automatic tracking and Hierarchical correlating of EventHubs messages

The Arcus message pump/router automatically makes sure that received Azure EventHubs event messages are tracked as request telemetry in Application Insights. 
If you also want the sender (dependency tracking) to be linked to the request, we provide a set of easy extensions on the `EventHubProducerClient` to make this happen.
For more information on dependency tracking, see the [Arcus Observability feature documentation on telemetry tracking](https://observability.arcus-azure.net/features/writing-different-telemetry-types/).

> 🚩 By default, the EventHubs message pump is using W3C correlation, not Hierarchical, which already allows automatic dependency tracking upon sending without additional configuration. If you want to use Hierarchical, please configure the correlation format in the [message pump configuration](./02-message-handling/03-event-hubs.md).

Internally, we enrich the `EventData` with the message correlation and track the entire operation as an Azure EventHubs dependency.
The result of this operation will result in a parent-child relationship between the dependency-request.

Following example shows how any business content (`Order`) can be wrapped automatically internally in a `EventHubs`, and send as a correlated tracked message to the Azure EventHubs resource:

```csharp
using Azure.Messaging.EventHubs;

Order[] orders = ... // Your business model.
MessageCorrelationInfo correlation = ... // Retrieved from your message handler implementation.
ILogger logger = ... // Your dependency injected logger from your application.

await using (var producer = new EventHubProducerClient("<eventhubs-connectionstring>", "<eventhubs-name>")
{
    await producer.SendAsync(orders, correlation, logger);
    // Output: {"DependencyType": "Azure Event Hubs", "DependencyId": "c55c7885-30c5-4785-ad15-a96e03903bfa", "TargetName": "<eventhubs-name>", "Duration": "00:00:00.2521801", "StartTime": "03/23/2020 09:56:31 +00:00", "IsSuccessful": true, "Context": []}
}
```

The dependency tracking can also be configured with additional options to your needs. 
You can also create your own `EventData` with one of the method overloads, so you have influence on the entire message's contents and application properties.

> ⚠ Note that changes to the application property names should also reflect in changes in the application properties at the receiving side, so that the message pump/router knows where it will find these correlation properties.

```csharp
await producer.SendAsync(orders, correlation, logger, options =>
{
    // The Azure EventHubs application property name where the message correlation transaction ID will be set.
    // Default: Transaction-Id
    options.TransactionIdPropertyName = "My-Transaction-Id";

    // The Azure EventHubs application property name where the dependency ID property will be set.
    // This ID is by default generated and added to both the dependency tracking as the message.
    // Default: Operation-Parent-Id
    options.UpstreamServicepropertyName = "My-UpstreamService-Id";

    // The Azure EventHubs application function to generate a dependency ID which will be added to both the message as the dependency tracking.
    // Default: GUID generation.
    options.GenerateDependencyId = () => $"dependency-{Guid.NewGuid()}";

    // The contextual information that will be used when tracking the Azure EventHubs dependency.
    // Default: empty dictionary.
    options.AddTelemetryContext(new Dictionary<string, object>
    {
        ["Additional_EventHubs_Info"] = "EventHubs-Info"
    });
});

EventData[] messages = ...
await sender.SendAsync(messages, ...);
```

## Simplify Creating EventHubs Messages

Starting from the message body, we provide a builder to quickly wrap the content in a valid Azure EventHubs `EventData` type that can be send.

```csharp
using Azure.Messaging.EventHubs;

Order order = new Order("order-id");
EventData message = EventDataBuilder.CreateForBody(order).Build(); 
```

We also provide additional, optional parameters during the creation:

```csharp
using Azure.Messaging.ServiceBus;

Order order = new Order("order-id");
EventData message =
  EventDataBuilder.CreateForBody(order, Encoding.UTF8)
                  .WithOperationId($"operation-{Guid.NewGuid()}")
                  .WithTransactionId($"transaction-{Guid.NewGuid()}")
                  .WithOperationParentId($"parent-{Guid.NewGuid()}")
                  .Build();
```

* `OperationId`: reflects the ID that identifies a single operation within a service. The passed ID will be set on the `eventData.CorrelationId` property.
* `TransactionId`: reflects the ID that identifies the entire transaction across multiple services. The passed ID will be set on the `eventData.Properties` dictionary with the key `"Transaction-Id"`, which is overridable.
* `OperationParentId`: reflecs the ID that identifies the sender of the event message to the receiver of the message (parent -> child). The passed ID will be set on the `eventData.Properties` dictionary with the key `Operation-Parent-Id`, which is overriable.

## Simplify Message Information Discovery

On receive, the Azure EventHubs event message contains a set of `.Properties` with additional information ie. correlation set form the `EventDataBuilder`.
This information can be accessed in a more simplified way:

```csharp
using Arcus.Messaging.Abstractions;
using Azure.Messaging.EventHubs;

EventData message = ...

// Extracted all correlation information from the `.Properties` and wrapped inside a valid correlation type.
MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

// Extract only the transaction identifier from the correlation information.
string transactionId = message.GetTransactionId();

// Extract a user property in a typed manner.
string myCustomPropertyValue = message.GetUserProperty<string>("my-custom-property-key");
```

## Simplify Message Context Information Discovery

On receive, the context in which the message is received contains a set of `.Properties` with additional information ie. encoding set from the `EventDataBuilder`.
This information can be access in a more simplified way:

```csharp
using Arcus.Messaging.Abstractions;
using Azure.Messaging.EventHubs;

EventProcessorClient processor = ... // Client that receives the message.
EventData message = ... // The received message.

// Creates a new messaging context from the message and processor, using an unique job ID to identify all message handlers that can handle this specific context.
AzureEventHubsMessageContext messageContext = message.GetMessageContext(processor, "my-job-id");

// Extract the encoding information from the `.Properties` and wrapped inside a valid `Encoding` type.
Encoding encoding = messageContext.GetMessageEncodingProperty();
```
