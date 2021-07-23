---
title: "Home"
layout: default
---

# Installation

[![NuGet Badge](https://buildstats.info/nuget/Arcus.Messaging.Abstractions)](https://www.nuget.org/packages/Arcus.Messaging.Abstractions/)

The features are available on NuGet:

```shell
PM > Install-Package Arcus.Messaging.Abstractions
```

# Features

- Support for using message pumps for the following brokers:
    - Azure Service Bus ([docs](features/message-pumps/service-bus) | [extensions](features/service-bus))
- Support for exposing TCP health probes to periodically check liveness/readiness of the host ([docs](features/tcp-health-probe))
- Customize message pumps ([docs](features/message-pumps/customization))
    - Fallback message handlers
- Using message pumps with Azure Functions
    - Azure Service Bus ([docs](features/message-pumps/service-bus-azure-functions))

## Guides

* Guide to migrate towards the v1.0 release ([docs](guides/migration-guide-v1.0))

## Older versions

- [v0.4](./../v0.4.0)
- [v0.3](./../v0.3.0)
- [v0.2](./../v0.2.0)
- [v0.1](./../v0.1.0)

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.messaging/blob/master/LICENSE)*
