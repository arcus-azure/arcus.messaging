# Migrate from v2.x to v3.0
Starting from v3.0, there are some major breaking changes related to the [lightweight exercise](https://github.com/arcus-azure/arcus.messaging/discussions/470) that the Messaging library gone through. This guide will make it easier for you to migrate towards this version from an older v2.x version.

## New Service Bus message pump registration
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

## New Service Bus message handler registration
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