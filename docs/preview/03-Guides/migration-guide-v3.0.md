# Migrate from v2.x to v3.0
Starting from v3.0, there are some major breaking changes related to the [lightweight exercise](https://github.com/arcus-azure/arcus.messaging/discussions/470) that the Messaging library gone through. This guide will make it easier for you to migrate towards this version from an older v2.x version.

## New Service bus message handler registration
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