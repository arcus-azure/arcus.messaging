# Migration guide towards v1.0
Starting from v1.0, there're some major breaking changes. To make it easier for you you migrate towards this new version, we have assembled an migration guide to help you in the process.

- [New Azure SDK](#new-azure-sdk)
  - [Package update](#package-update)
  - [Service Bus message update for fallback message handlers](#service-bus-message-update-for-fallback-message-handlers)
  - [Message correlation inforamation update](#message-correlation-inforamation-update)
- [Moved message handler types to abstractions namespaces](#moved-message-handler-types-to-abstraction-namespaces)
- [Renamed fallback message handler operations](#renamed-fallback-message-handler-operations)
- [Fluent API discovery for message handling](#fluent-api-discovery-for-message-handling)

## New Azure SDK
We have chosen to also update our library to the new Azure SDK when interacting with the Azure Service Bus ([#159](https://github.com/arcus-azure/arcus.messaging/discussions/159)). This package update has some consequences on our library.

### Package update
The `Microsoft.Azure.ServiceBus` NuGet packge is now completly removed from the library and is changed by the `Arcus.Messaging.ServiceBus` NuGet package. This means that possible compile errors can occur when using types or signatures that were only available in this older package.

### Service Bus message update for fallback message handlers
The `AzureServiceBusFallbackMessagHandler<>` abstract type has an updated signature as it now uses the new `ServiceBusReceivedMessage` instead of the `Message` when providing a fallback for a message handler pipeline:

```diff
- using Microsoft.Azure.ServiceBus;
+ using Azure.Messaging.ServiceBus;

public class OrderFallbackMessageHandler : AzureServiceBusFallbackMessageHandler
{
    public override async Task ProcessMessageAsync(
-       Message message,
+       ServiceBusReceivedMessage message,
        AzureServiceBusMessageContext azureMessageContext,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
    {
        ...
    }
}
```

> Note that some Service Bus-specific operations were renamed to, see [this section](#renamed-fallback-message-handler-operations) for more info.

### Message correlation inforamation update
The correlation information model `MessageCorrelationInfo` could previously be extracted from the [`Message` of the old SDK](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.servicebus.message?view=azure-dotnet) with the extention `message.GetCorrelationInfo()`.

This new version works with the new `ServiceBusReceivedMessage`, so the correlation extension is also moved.

```diff
- using Microsoft.Azure.ServiceBus;
+ using Azure.Messaging.ServiceBus;

- Message message = ...
+ ServiceBusReceivedMessage message = ...

message.GetCorrelationInfo();
```

## Moved message handler types to abstraction namespaces
All your Azure Service Bus message handlers implementations will probably give compile errors. This is caused by the breaking change that moved all the 'message handler'-related types towards abstractions namespaces ([#153](https://github.com/arcus-azure/arcus.messaging/issues/153)).
Practically, this means that these namespaces are renamed:

* `Arcus.Messaging.Pumps.MessagingHandling` becomes `Arcus.Messaging.Abstractions.MessageHandling`
* `Arcus.Messaging.Pumps.ServiceBus.MessageHandling` becomes `Arcus.Messaging.Abstractions.MessageHandling.ServiceBus`
    * Also the `IAzureServiceBusMessageHandler<>` is moved to this new namespace.

This has effect on the `IAzureServiceBusMessageHandler<>`, `IAzureServiceBusFallbackMessageHandler` interfaces; and the `AzureServiceBusMessageHandler<>`, `AzureServiceBusFallbackMessageHandler<>` abstract types.
Following example shows how older versions has now uses non-existing namespaces in the. 

```diff
- using Arcus.Messaging.Pumps.ServiceBus;
+ using Arcus.Messaging.Abstractions.MessageHandling.ServiceBus;

public class OrderMessageHandler : IAzureServiceBusMessageHandler<Order>
{
    ...
}
```

## Renamed fallback message handler operations
Any of your custom fallback message handler implementations that inherit from the `AzureServiceBusFallbackMessageHandler` abstract type will probably cause compile errors. This is caused by our change that renamed the Service Bus-specific operations on this abstract type ([#194](https://github.com/arcus-azure/arcus.messaging/issues/194)). 

```diff
public class OrderFallbackMessageHandler : AzureServiceBusFallbackMessageHandler<Order>
{
    public override Task ProcessMessageAsync(...)
    {
-       base.CompleteAsync(message);
+       base.CompleteMessageAsync(message);
    }
}
```

## Fluent API discovery for message handling
It's possible that some of your Azure Service Bus message handler registrations give compile errors. This is caused because we have introduced a dedicated type as return type when registering an Azure Service Bus message pump ([#152](https://github.com/arcus-azure/arcus.messaging/issues/152)). This dedicated type helps with the discovery of the available message handler registration options.

Following example shows how older versions could register the message handler directly on the `services`, while now they're only available after registering the message pump:

```diff
- services.AddServiceBusQueueMessagePump(...);
- services.WithServiceBusMessageHandler<OrderMessageHandler, Order>();

+ services.AddServiceBusQueueMessagePump(...)
+         .WithServiceBusMessageHandler<OrderMessageHandler, Order>();
```