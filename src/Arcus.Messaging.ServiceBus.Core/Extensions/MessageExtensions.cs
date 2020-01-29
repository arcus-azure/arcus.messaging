using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.ServiceBus
{
    public static class MessageExtensions
    {
        /// <summary>
        ///     Gets the user property with a given <paramref name="key"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetUserProperty<T>(this Message message, string key)
        {
            if (message.UserProperties.TryGetValue(key, out object value))
            {
                if (value is T typed)
                {
                    return typed;
                }

                throw new InvalidCastException(
                    $"The found user property with the key: '{key}' in the Service Bus message was not of the expected type: '{typeof(T).Name}'");
            }

            throw new KeyNotFoundException(
                $"No user property with the key: '{key}' was found in the Service Bus message");
        }
    }
}