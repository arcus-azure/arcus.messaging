---
title: "Home"
layout: default
permalink: /
redirect_from:
 - /index.html
---

# Installation

[![NuGet Badge](https://buildstats.info/nuget/Arcus.Messaging.Abstractions?packageVersion=0.3.0)](https://www.nuget.org/packages/Arcus.Messaging.Abstractions/0.3.0)

The features are available on NuGet:

```shell
PM > Install-Package Arcus.Messaging.* -Version 0.3.0
```

# Features

- Support for using message pumps for the following brokers:
    - Azure Service Bus ([docs](features/message-pumps/service-bus) | [extensions](features/service-bus))
- Support for exposing TCP health probes to periodically check liveness/readiness of the host ([docs](features/tcp-health-probe))
- Customize message pumps ([docs](features/message-pumps/customization))
    - Fallback message handlers

## Older versions

- [v0.3.0](v0.3.0)
- [v0.2.0](v0.2.0)
- [v0.1.0](v0.1.0)

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.messaging/blob/master/LICENSE)*
