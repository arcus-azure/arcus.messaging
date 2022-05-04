---
title: "Arcus - Messaging"
layout: default
slug: /
sidebar_label: Welcome
---

# Introduction

Arcus Messaging is a library that helps with the integration of messaging systems like queues or event subscriptions, with application message handling that describes the business.
The library makes sure that the developer only has to concern themselves with the actual processing of messages instead of spending time with message peaking, connections, deserialization, and other infrastructure code that takes up time.

Currently, we only support Azure Service Bus messaging but there's more to come!
Take a look at the [Azure Service Bus Worker Service docs](./02-Features/02-message-handling/01-service-bus.md) to learn more about setting up your own message handling system.

# Installation

[![NuGet Badge](https://buildstats.info/nuget/Arcus.Messaging.Abstractions)](https://www.nuget.org/packages/Arcus.Messaging.Abstractions/)

The features are available on NuGet:

```shell
PM > Install-Package Arcus.Messaging.Abstractions
```

# Features

- Support for using message handling for the following brokers:
  - Azure Service Bus
    - Worker Service ([docs](./02-Features/02-message-handling/01-service-bus.md))
    - Azure Functions ([docs](./02-Features/02-message-handling/02-service-bus-azure-functions.md)) 
- Support for exposing TCP health probes to periodically check liveness/readiness of the host ([docs](./02-Features/03-tcp-health-probe.md))
- Azure Service Bus extensions ([docs](./02-Features/04-service-bus-extensions.md))
- Customize general message handling ([docs](./02-Features/02-message-handling/03-customize-general.md))

## Guides

* Migrate from v0.x to v1.0 ([docs](./03-Guides/migration-guide-v1.0.md))

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.messaging/blob/master/LICENSE)*
