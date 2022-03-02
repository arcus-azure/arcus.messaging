---
title: "Azure Service Bus Extensions"
layout: default
---

# Azure Service Bus Features

We provide several additional features related to message creation and message/context discoverability.

## Installation

This features requires to install our NuGet package:

```shell
PM > Install-Package Arcus.Messaging.ServiceBus.Core
```

## Simplify Creating Service Bus Messages

Starting from the message body, we provide an extension to quickly wrap the content in a valid Azure Service Bus `Message` type that can be send.

```csharp
using Azure.Messaging.ServiceBus;

string rawString = "Some raw content";
ServiceBusMessage stringMessage = rawString.AsServiceBusMessage();

Order order = new Order("some order id");
Message orderMessage = order.AsServiceBusMessage();
```

We also provide additional, optional parameters during the creation:

```csharp
using Azure.Messaging.ServiceBus;

byte[] rawBytes = new [] { 0x54, 0x12 };
ServiceBusMessage = byteMessage = rawBytes.AsServiceBusMessage(
    operationId: Guid.NewGuid().ToString(),
    transactionId: Guid.NewGuid().ToString(),
    encoding: Encoding.UTF8);
```

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

// Extract the encoding information from the `.ApplicationProperties` and wrapped inside a valid `Encoding` type.
MessageContext messageContext = ...
Encoding encoding = messageContext.GetMessageEncodingProperty();
```

[&larr; back](/)