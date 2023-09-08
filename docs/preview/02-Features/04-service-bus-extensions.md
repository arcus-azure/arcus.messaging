---
title: "Azure Service Bus Extensions"
layout: default
---

# Azure Service Bus Extensions

We provide several additional features related to message creation/sending and message/context discoverability.

## Installation

This features requires to install our NuGet package:

```shell
PM > Install-Package Arcus.Messaging.ServiceBus.Core
```

## Using Arcus secret store when registering the Service Bus client

When registering a `ServiceBusClient` via [Azure's client registration process](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme), the library provides an extension to pass-in a secret name instead of directly passing the Azure Service Bus connection string.
This secret name will correspond with a registered secret in the [Arcus secret store](https://security.arcus-azure.net/features/secret-store) that holds the Azure Service Bus connection string.

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

        // Adding Service Bus client with secret in Arcus secret store.
        services.AddAzureClients(clients => clients.AddServiceBusClient(connectionStringSecretName: "<your-secret-name>"));
    }
}
```

🥇 Adding your Azure Service Bus client this way helps separating application configuration from sensitive secrets. For more information on the added-values of the Arcus secret store, see [our dedicated documentation page](https://security.arcus-azure.net/features/secret-store).

## Automatic tracking and Hierarchical correlating of Service Bus messages

The Arcus message pump/router automatically makes sure that received Azure Service Bus messages are tracked as request telemetry in Application Insights. 
If you also want the sender (dependency tracking) to be linked to the request, we provide a set of easy extensions on the `ServiceBusSender` to make this happen.
For more information on dependency tracking, see the [Arcus Observability feature documentation on telemetry tracking](https://observability.arcus-azure.net/features/writing-different-telemetry-types/).

> 🚩 By default, the Service Bus message pump is using W3C correlation, not Hierarchical, which already allows automatic dependency tracking upon sending without additional configuration. If you want to use Hierarchical, please configure the correlation format in the [message pump configuration](./02-message-handling/01-service-bus.md).

Internally, we enrich the `ServiceBusMessage` with the message correlation and track the entire operation as an Azure Service Bus dependency.
The result of this operation will result in a parent-child relationship between the dependency-request.

Following example shows how any business content (`Order`) can be wrapped automatically internally in a `ServiceBusMessage`, and send as a correlated tracked message to the Azure Service Bus resource:

```csharp
using Azure.Messaging.ServiceBus;

Order order = ... // Your business model.
MessageCorrelationInfo correlation = ... // Retrieved from your message handler implementation.
ILogger logger = ... // Your dependency injected logger from your application.

await using (var client = new ServiceBusClient(...))
await using (ServiceBusSender sender = client.CreateSender("my-queue-or-topic"))
{
    await sender.SendMessageAsync(order, correlation, logger);
    // Output: {"DependencyType": "Azure Service Bus", "DependencyId": "c55c7885-30c5-4785-ad15-a96e03903bfa", "TargetName": "ordersqueue", "Duration": "00:00:00.2521801", "StartTime": "03/23/2020 09:56:31 +00:00", "IsSuccessful": true, "Context": {"EntityType": "Queue"}}
}
```

The dependency tracking can also be configured with additional options to your needs. 
You can also create your own `ServiceBusMessage` with one of the method overloads, so you have influence on the entire message's contents and application properties.

> ⚠ Note that changes to the application property names should also reflect in changes in the application properties at the receiving side, so that the message pump/router knows where it will find these correlation properties.

```csharp
await sender.SendMessageAsync(order, correlation, logger, options =>
{
    // The Azure Service Bus application property name where the message correlation transaction ID will be set.
    // Default: Transaction-Id
    options.TransactionIdPropertyName = "My-Transaction-Id";

    // The Azure Service Bus application property name where the dependency ID property will be set.
    // This ID is by default generated and added to both the dependency tracking as the message.
    // Default: Operation-Parent-Id
    options.UpstreamServicepropertyName = "My-UpstreamService-Id";

    // The Azure Service Bus application function to generate a dependency ID which will be added to both the message as the dependency tracking.
    // Default: GUID generation.
    options.GenerateDependencyId = () => $"dependency-{Guid.NewGuid()}";

    // The dictionary containing any additional contextual inforamtion that will be used when tracking the Azure Service Bus dependency (Default: empty dictionary).
    options.AddTelemetryContext(new Dictionary<string, object>
    {
        ["My-ServiceBus-custom-key"] = "Any additional information"
    });
});

ServiceBusMessage message = ...
await sender.SendMessageAsync(message, ...);
```

We also support multiple message bodies or messages:

```csharp
Order[] orders = ...
await sender.SendMessagesAsync(orders, ...);

ServiceBusMessage[] messages = ...
await sender.SendMessagesAsync(mesages, ...);
```

## Simplify Creating Service Bus Messages

Starting from the message body, we provide a builder to quickly wrap the content in a valid Azure Service Bus `ServiceBusMessage` type that can be send.

```csharp
using Azure.Messaging.ServiceBus;

Order order = new Order("order-id");
ServiceBusMessage message = ServiceBusMessageBuilder.CreateForBody(order).Build(); 
```

We also provide additional, optional parameters during the creation:

```csharp
using Azure.Messaging.ServiceBus;

ServiceBusMessage message =
  ServiceBusMessageBuilder.CreateForBody(order, Encoding.UTF8)
                          .WithOperationId($"operation-{Guid.NewGuid()}")
                          .WithTransactionId($"transaction-{Guid.NewGuid()}")
                          .WithOperationParentId($"parent-{Guid.NewGuid()}")
                          .Build();
```


* `OperationId`: reflects the ID that identifies a single operation within a service. The passed ID will be set on the `message.CorrelationId` property.
* `TransactionId`: reflects the ID that identifies the entire transaction across multiple services. The passed ID will be set on the `message.ApplicationProperties` dictionary with the key `"Transaction-Id"`, which is overridable.
* `OperationParentId`: reflecs the ID that identifies the sender of the event message to the receiver of the message (parent -> child). The passed ID will be set on the `message.ApplicationProperties` dictionary with the key `Operation-Parent-Id`, which is overriable.

## Simplify Message Information Discovery

On receive, the Azure Service Bus message contains a set of `.ApplicationProperties` with additional information ie. correlation.
This information can be accessed in a more simplified way:

```csharp
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;

ServiceBusMessage message = ...

// Extracted all correlation information from the `.ApplicationProperties` and wrapped inside a valid correlation type.
MessageCorrelationInfo correlationInfo = message.GetCorrelationInfo();

// Extract only the transaction identifier from the correlation information.
string transactionId = message.GetTransactionId();

// Extract a user property in a typed manner.
string myCustomPropertyValue = message.GetUserProperty<string>("my-custom-property-key");
```

## Simplify Message Context Information Discovery

On receive, the context in which the message is received contains a set of `.ApplicationProperties` with additional information ie. encoding.
This information can be access in a more simplified way:

```csharp
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;

// Creates a new messaging context from the message, using an unique job ID to identify all message handlers that can handle this specific context.
AzureServiceBusMessageContext messageContext = message.GetMessageContext("my-job-id", ServiceBusEntityType.Topic);

// Extract the encoding information from the `.ApplicationProperties` and wrapped inside a valid `Encoding` type.
MessageContext messageContext = ...
Encoding encoding = messageContext.GetMessageEncodingProperty();
```
