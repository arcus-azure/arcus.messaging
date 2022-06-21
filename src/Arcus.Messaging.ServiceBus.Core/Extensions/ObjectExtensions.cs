using System;
using System.Text;
using Arcus.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using GuardNet;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.ServiceBus
{
    /// <summary>
    /// Extensions on message bodies to more easily create <see cref="ServiceBusMessage"/>s from them.
    /// </summary>
    public static class ObjectExtensions
    {
        private const string JsonContentType = "application/json";

    }
}