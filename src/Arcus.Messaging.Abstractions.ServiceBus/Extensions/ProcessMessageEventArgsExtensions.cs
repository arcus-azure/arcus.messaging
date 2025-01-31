using System;
using System.Reflection;
using Arcus.Messaging.Abstractions.MessageHandling;

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.ServiceBus
{
    /// <summary>
    /// Extensions on the <see cref="ProcessMessageEventArgs"/> to get the original Service Bus Receiver instance.
    /// </summary>
    public static class ProcessMessageEventArgsExtensions
    {
        /// <summary>
        /// Gets the original <see cref="ServiceBusReceiver"/> instance from the Azure Service Bus event <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The event args that's used to process an Azure Service Bus message.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="args"/> is <c>null</c>.</exception>
        /// <exception cref="TypeNotFoundException">Thrown when the no Azure Service Bus receiver could be found on the <paramref name="args"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no value could be found for the Azure Service Bus receiver on the <paramref name="args"/>.</exception>
        /// <exception cref="InvalidCastException">Thrown when the value for the Azure Service Bus receiver on the <paramref name="args"/> wasn't the expected type.</exception>
        [Obsolete("Service Bus receiver is used internally instead, no need to go via the processor")]
        public static ServiceBusReceiver GetServiceBusReceiver(this ProcessMessageEventArgs args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            FieldInfo receiverField = args.GetType().GetField("_receiver", BindingFlags.Instance | BindingFlags.NonPublic);
            if (receiverField is null)
            {
                throw new TypeNotFoundException(
                    "Could not find an Azure Service Bus receiver instance on the current received event args");
            }

            object receiverValue = receiverField.GetValue(args);
            if (receiverValue is null)
            {
                throw new InvalidOperationException(
                    "Could not find any value for the Azure Service Bus receiver instance on the current received event args");
            }

            if (receiverValue is ServiceBusReceiver receiver)
            {
                return receiver;
            }

            throw new InvalidCastException(
                "Could not find a value for the Azure Service Bus receiver instance with the expected type on the current received event args");
        }
    }
}
