# Migrate from v2.x to v3.0
Starting from v3.0, there are some major breaking changes related to the [lightweight exercise](https://github.com/arcus-azure/arcus.messaging/discussions/470) that the Messaging library gone through. This guide will make it easier for you to migrate towards this version from an older v2.x version.

## General
* 🗑️ .NET 6 & .NET Standard 2.1 support is removed.
* 🗑️ Transient `Newtonsoft.Json` dependency is replaced by built-in `System.Text.Json`
* 🗑️ Transient `GuardNET` dependency is replaced by built-in argument checking.
* 🗑️ Removed support for deprecated **Hierarchical** correlation format in favor of the new **W3C** format.
* 🗑️ Built-in 'general messaging' functionality is removed affects following types/extensions:
  * `IMessageRouter`
  * `MessageRouter`
  * `IMessageHandler<>`
  * `IFallbackMessageHandler`
  * `FallbackMessageHandler`
  * `IServiceCollection.WithMessageHandler(...)`
  * `IServiceCollection.WithFallbackMessageHandler(...)`

## 👋 Arcus.Messaging.*EventHubs\*
All Azure EventHubs-related functionality has been removed from v3.0. This means that the following packages are archived on NuGet and stop receiving support from Arcus:
* 📦 **Arcus.Messaging.Abstractions.EventHubs**
* 📦 **Arcus.Messaging.AzureFunctions.EventHubs**
* 📦 **Arcus.Messaging.EventHubs.Core**
* 📦 **Arcus.Messaging.Pumps.EventHubs**

## 📦 Arcus.Messaging.*ServiceBus\*
### 🗑️ Removed functionality
* Removed built-in Azure Functions support. Both the `Arcus.Messaging.AzureFunctions.ServiceBus` package as the built-in service-to-service correlation support for Azure Functions has been removed.
* Removed fallback message handler functionality in favor of using [custom message settlement](#-new-service-bus-message-settlement).
* Removed transient `Arcus.Security.*` dependency in favor of [custom message pump registration](#-new-service-bus-message-pump-registration).

### ✨ New Service Bus message pump registration
Previously, the registration of the Azure Service Bus message pump involved navigating through the many available extensions, making it rather tedious to find the right authentication mechanism.

Starting from v3.0, the registrations of the Azure Service Bus message pump is simplified with two simple approaches:
1. Register message pump with custom `TokenCredential`.
2. Register message pump with custom implementation factory, that creates a `ServiceBusClient`.

```diff
// #1 Via custom `TokenCredential`

- services.AddServiceBusQueueMessagePumpUsingManagedIdentity("<queue-name>", "<namespace>")
+ services.AddServiceBusQueueMessagePump("<queue-name>", "<namespace>", new ManagedIdentityCredential())

// #2 Via custom implementation factory (using Arcus secret store)

services.AddSecretStore(...);

- services.AddServiceBusTopicMessagePump("<topic-name>", "<subscription-name>", (ISecretProvider secrets) =>
- {
-       return secrets.GetSecret("ConnectionString"));
- })
+ services.AddServiceBusTopicMessagePump("<topic-name>", "<subscription-name>", (IServiceProvider services) =>
+ {
+       var secrets = services.GetRequiredService<ISecretProvider>();        
+       return new ServiceBusClient(secrets.GetSecret("ConnectionString"));
+})
```

### ✨ New Service Bus message handler registration
Previously, the registration of custom `IAzureServiceBusMessageHandler<>` implementations involved navigating through the many available extensions, making it rather tedious to find the right overload.

Starting from v3.0, the registration of custom message handlers is simplified with an options model, where all message routing additions can be added.

```diff
services.AddServiceBusQueueMessagePump(...)
        .WithServiceBusMessageHandler<OrdersRegistrationMessageHandler, Order>(
-           messageContextFilter: ctx => ctx.Properties["MessageType"] == "Order",
-           messageBodyFiler: order => order.Type == OrderType.Registration,
-           messageBodySerializer: new OrdersXmlMessageBodySerializer());
+           options =>
+           {
+               options.AddMessageContextFilter(ctx => ctx.Properties["MessageType"] == "Order")
+                      .AddMessageBodyFilter(order => order.Type == OrderType.Registration)
+                      .AddMessageBodySerializer(new OrdersXmlMessageBodySerializer())
+           });

```

### ✨ New Service Bus message settlement
Previous versions used dedicated 'template classes' that custom message handlers should inherit from to do custom Azure Service Bus message settlement (complete, dead-letter, abandon).

Starting from v3.0, the available operations are moved to the `AzureServiceBusMessageContext`. Making your custom message handlers much more accessible and flexible.

```diff
public class OrderServiceBusMessageHandler
-    : AzureServiceBusMessageHandler<Order>
+    : IAzureServiceBusMessageHandler<Order>
{
    public OrderServiceBusMessageHandler(ILogger<OrderServiceBusMessageHandler> logger)
-        : base(logger)
    {

    }

-    public override async Task ProcessMessageAsync(
+    public async Task ProcessMessageAsync(
        Order order,
        AzureServiceBusMessageContext messageContext,
        MessageCorrelationInfo messageCorrelation,
        CancellationToken cancellation)
    {
-        await DeadLetterMessageAsync("Reason: Unsupported", "Message type is not supported");
+       await messageContext.DeadLetterMessageAsync("Reason: Unsupported", "Message type is not supported");
    }
}
```