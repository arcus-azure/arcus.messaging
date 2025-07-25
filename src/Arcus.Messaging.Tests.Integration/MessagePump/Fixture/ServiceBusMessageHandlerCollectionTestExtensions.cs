using System;
using Arcus.Messaging.Abstractions.ServiceBus.MessageHandling;
using Arcus.Messaging.Tests.Core.Messages.v1;
using Arcus.Messaging.Tests.Workers.MessageHandlers;
using Bogus;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class ServiceBusMessageHandlerCollectionTestExtensions
    {
        private static readonly Faker Bogus = new();

        internal static ServiceBusMessageHandlerCollection WithUnrelatedServiceBusMessageHandler(this ServiceBusMessageHandlerCollection collection)
        {
            switch (Bogus.Random.Int(1, 4))
            {
                case 1:
                    collection.WithServiceBusMessageHandler<OrderBatchMessageHandler, OrderBatch>(WithUnrelatedHandlerFiltering);
                    break;

                case 2:
                    collection.WithServiceBusMessageHandler<CustomerMessageHandler, Customer>(WithUnrelatedHandlerFiltering);
                    break;

                case 3:
                    collection.WithServiceBusMessageHandler<ShipmentAzureServiceBusMessageHandler, Shipment>();
                    break;

                case 4:
                    collection.WithServiceBusMessageHandler<PassThruOrderMessageHandler, Order>(handler =>
                    {
                        (bool matchesContext, bool matchesBody) =
                            Bogus.PickRandom((false, true), (true, false), (false, false));

                        handler.AddMessageContextFilter(_ => matchesContext);
                        handler.AddMessageBodyFilter(_ => matchesBody);
                    });
                    break;
            }

            return collection;
        }

        private static void WithUnrelatedHandlerFiltering<T>(ServiceBusMessageHandlerOptions<T> options)
        {
            switch (Bogus.Random.Int(0, 2))
            {
                case 0:
                    break;

                case 1:
                    options.AddMessageContextFilter(_ => false);
                    break;

                case 2:
                    options.AddMessageBodyFilter(_ => false);
                    break;
            }

            options.AddMessageContextFilter(_ => true)
                   .AddMessageBodyFilter(_ => true);
        }

        internal static ServiceBusMessageHandlerCollection WithMatchedServiceBusMessageHandler(
            this ServiceBusMessageHandlerCollection collection,
            Action<ServiceBusMessageHandlerOptions<Order>> configureOptions = null)
        {
            return WithMatchedServiceBusMessageHandler<WriteOrderToDiskAzureServiceBusMessageHandler>(collection, configureOptions);
        }

        internal static ServiceBusMessageHandlerCollection WithMatchedServiceBusMessageHandler<TMessageHandler>(
            this ServiceBusMessageHandlerCollection collection,
            Action<ServiceBusMessageHandlerOptions<Order>> configureOptions = null)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<Order>
        {
            return collection.WithServiceBusMessageHandler<TMessageHandler, Order>(configureOptions);
        }

        internal static ServiceBusMessageHandlerCollection WithMatchedServiceBusMessageHandler<TMessageHandler, TMessage>(
            this ServiceBusMessageHandlerCollection collection,
            Action<ServiceBusMessageHandlerOptions<TMessage>> configureOptions = null)
            where TMessageHandler : class, IAzureServiceBusMessageHandler<TMessage>
            where TMessage : class
        {
            return collection.WithServiceBusMessageHandler<TMessageHandler, TMessage>(configureOptions);
        }
    }
}
