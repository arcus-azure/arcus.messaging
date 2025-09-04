# Migrate from v2.x to v3.0
Starting from v3.0, there are some major breaking changes related to the [lightweight exercise](https://github.com/arcus-azure/arcus.messaging/discussions/470) that the Messaging library gone through. This guide will make it easier for you to migrate towards this version from an older v2.x version.

## General
* 🗑️ .NET 6 & .NET Standard 2.1 support is removed.
* 🗑️ Transient `Newtonsoft.Json` dependency is replaced by built-in `System.Text.Json`
* 🗑️ Transient `GuardNET` dependency is replaced by built-in argument checking.
* 🗑️ Removed support for deprecated **Hierarchical** correlation format in favor of the new **W3C** format.
* 🗑️ 'Message pump lifetime' is removed, in favor of circuit-breaker functionality, which affects following types:
  * `IMessagePumpLifetime`
  * `DefaultMessagePumpLifetime`
* 🗑️ 'Restart message pump' functionality is removed, which affects following types:
  * `IRestartableMessagePump`
* 🗑️ Built-in 'general messaging' functionality is removed affects following types/extensions:
  * `IMessageRouter`
  * `MessageRouter`
  * `IMessageHandler<>`
  * `IFallbackMessageHandler`
  * `FallbackMessageHandler`
  * `MessageHandlerCollection.WithMessageHandler(...)`
  * `MessageHandlerCollection.WithFallbackMessageHandler(...)`

## 👋 Arcus.Messaging.*EventHubs\*
All Azure EventHubs-related functionality has been removed from v3.0. This means that the following packages are archived on NuGet and stop receiving support from Arcus:
* 📦 **Arcus.Messaging.Abstractions.EventHubs**
* 📦 **Arcus.Messaging.AzureFunctions.EventHubs**
* 📦 **Arcus.Messaging.EventHubs.Core**
* 📦 **Arcus.Messaging.Pumps.EventHubs**

## 📦 Arcus.Messaging.*ServiceBus\*
### 🗑️ Removed functionality
* Removed built-in Azure Functions support. Both the `Arcus.Messaging.AzureFunctions.ServiceBus` package as the built-in service-to-service correlation and message router registration support for Azure Functions has been removed.
* Removed fallback message handler functionality in favor of using [custom message settlement](#-new-service-bus-message-settlement). This means that the following types/extensions are removed:
  * `AzureServiceBusFallbackMessageHandler<>`
  * `ServiceBusMessageHandlerCollection.WithServiceBusFallbackMessageHandler(...)`
* Removed `IRestartableMessagePump` implementation for `AzureServiceBusMessagePump`
* Removed transient `Arcus.Security.*` dependency in favor of [custom message pump registration](#-new-service-bus-message-pump-registration).
  * Removed `DefaultAzureServiceBusManagementAuthentication`/`IAzureServiceBusManagementAuthentication`: allowed to authenticate the management client via Arcus.Security.
* Removed `AzureServiceBusClient` that allowed rotation of connection strings.
* Removed transient `Arcus.Observability.*` dependency in favor of [custom message correlation scopes](#-new-service-bus-message-correlation). This means that the following types are affected:
  * Removed `(I)MessageCorrelationInfoAccessor`: message correlation is already available via message handlers.
  * Removed `MessageCorrelationResult`: in favor of the new `MessageOperationResult`.
  * `MessageCorrelationInfo` is separated from parent `CorrelationInfo` (originates from Arcus.Observability)
* Removed **Arcus.Messaging.ServiceBus.Core** package and transient dependency for **Arcus.Messaging.Pumps.ServiceBus**. This means that the following types/extensions are removed:
  * `ServiceBusMessageBuilder`
  * `ServiceBusSenderMessageCorrelationOptions`
  * `ServiceBusSender.SendMessageAsync(..., CorrelationInfo, ILogger)`
  * `AzureClientFactoryBuilder.AddServiceBusClient(connectionStringSecretName)`
  * `ServiceBusReceivedMessage.GetCorrelationInfo`
  * `ServiceBusReceivedMessage.GetApplicationProperty`
  * `MessageContext.GetMessageEncodingProperty`

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
        .WithServiceBusMessageHandler<OrderServiceBusMessageHandler, Order>(
-           messageContextFilter: ctx => ctx.Properties["MessageType"] == "Order",
-           messageBodyFiler: order => order.Type == OrderType.Registration,
-           messageBodySerializer: new OrdersXmlMessageBodySerializer());
+           options =>
+           {
+               options.AddMessageContextFilter(ctx => ctx.Properties["MessageType"] == "Order")
+                      .AddMessageBodyFilter(order => order.Type == OrderType.Registration)
+                      .UseMessageBodySerializer(new OrdersXmlMessageBodySerializer())
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
-       await DeadLetterMessageAsync("Reason: Unsupported", "Message type is not supported");
+       await messageContext.DeadLetterMessageAsync("Reason: Unsupported", "Message type is not supported");
    }
}
```

:::note[prepare already for v4.0]
In the same fashion, message routing and message pumps have been placed more closely together since no separate use is by default supported. Because of this, the `AutoComplete` option has been moved to the message routing options. The original location has been deprecated in v3.0 and will be removed in v4.0.

```diff
services.AddServiceBusQueueMessagePump(..., options =>
{
-   options.AutoComplete = false;
+   options.Routing.AutoComplete = false;
})
```
:::

### ✨ New Service Bus message correlation
Previous versions hard-linked the message correlation with the required use of **Arcus.Observability** and **Serilog** to always track telemetry in Azure Application Insights.

The v3.0 version has radically removed this hard-link by introducing an `IServiceBusMessageCorrelationScope` interface that allows 'custom correlation implementations' to implement their own message correlation.

> 👉 This allows us to easily support **OpenTelemetry** in the future, as well.

To still benefit from the original W3C message correlation tracking with **Arcus.Observability** and **Serilog**, please follow these steps:

* 📦 Install the **Arcus.Messaging.ServiceBus.Telemetry.Serilog** package.
* 🔎 Navigate to the setup code that registers the Azure Service Bus message pump and its message handlers.
* 🔨 Register Serilog as the message correlation system by adding this line:
  ```diff
  services.AddServiceBusTopicMessagePump(...)
  +       .UseServiceBusSerilogRequestTracking()           
          .WithServiceBusMessageHandler<...>()
          .WithServiceBusMessageHandler<...>();
  ```
* 👀 Check that the `TelemetryClient` is registered in the application services (registering Azure Application Insights services is not done automatically anymore).
* 🎉 The original (< v3.0) message correlation is now restored.

We expect other kinds of message correlation registrations in the future. Switching between them would be a matter of choosing the correct `.WithServiceBus...RequestTracking()`.

## 📦 Arcus.Messaging.Health
### 🎯 Direct use of TCP port instead of indirect `IConfiguration` key
When registering the health report TCP probe in previous versions, the TCP port needed to be available in the application's configuration. This created an unnecessary hard-link between health reporting and configuration registration.

Starting from v3, the TCP port can be passed directly when registering the TCP health probe.

```diff
var builder = Host.CreateDefaultBuilder();

- builder.Services.AddTcpHealthProbes("<tcp-health-port-key>", 
-       configureHealthChecks: health =>
-       {
-               health.AddCheck("healthy", () => HealthCheckResult.Healthy());
-       });
+ builder.Services.AddHealthChecks()
+                 .AddCheck("healthy", () => HealthCheckResult.Healthy())
+                 .AddTcpHealthProbe(tcpPort: 5050);
```