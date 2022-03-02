---
title: "Arcus - Messaging"
layout: default
slug: /
sidebar_label: Welcome
---

# Installation

[![NuGet Badge](https://buildstats.info/nuget/Arcus.Messaging.Abstractions)](https://www.nuget.org/packages/Arcus.Messaging.Abstractions/)

The features are available on NuGet:

```shell
PM > Install-Package Arcus.Messaging.Abstractions
```

# Features

- Support for using message pumps for the following brokers:
    - Azure Service Bus ([docs](./features/message-pumps/service-bus.md) | [extensions](./features/service-bus.md))
- Support for exposing TCP health probes to periodically check liveness/readiness of the host ([docs](./features/tcp-health-probe.md))
- Customize message pumps ([docs](./features/message-pumps/customization.md))
    - Fallback message handlers
- Using message pumps with Azure Functions
    - Azure Service Bus ([docs](./features/message-pumps/service-bus-azure-functions.md))

## Guides

* Migrate from v0.x to v1.0 ([docs](./guides/migration-guide-v1.0.md))

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.messaging/blob/master/LICENSE)*
