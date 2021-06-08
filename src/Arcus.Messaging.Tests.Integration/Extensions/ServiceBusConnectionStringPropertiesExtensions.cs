using System;
using System.Reflection;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Azure.Messaging.ServiceBus
{
    /// <summary>
    /// Extensions on the <see cref="ServiceBusConnectionStringProperties"/> to extract internal data.
    /// </summary>
    public static class ServiceBusConnectionStringPropertiesExtensions
    {
        /// <summary>
        /// Gets the namespace-scoped Azure Service Bus connection string representation of the current set of <paramref name="properties"/>.
        /// </summary>
        /// <param name="properties">The Azure Service Bus property set of the separate connection string parts.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="properties"/> is <c>null</c>
        ///     or doesn't contain the necessary properties or methods to determine the namespace-scoped connection string.
        /// </exception>
        public static string GetNamespaceConnectionString(this ServiceBusConnectionStringProperties properties)
        {
            Guard.NotNull(properties, nameof(properties), "Requires an Azure Service Bus properties instance to determine the namespace-scoped connection string");
            
            string originalEntityPath = properties.EntityPath;
            SetEntityPath(properties, entityPath: null);

            string connectionString = GetConnectionString(properties);
            SetEntityPath(properties, originalEntityPath);

            return connectionString;
        }

        private static void SetEntityPath(ServiceBusConnectionStringProperties properties, string entityPath)
        {
            const BindingFlags internalScope = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty;
            Type propertiesType = properties.GetType();
            PropertyInfo entityPathProperty = propertiesType.GetProperty(nameof(properties.EntityPath));

            Guard.NotNull(entityPathProperty, nameof(entityPathProperty), 
                $"Requires a '{nameof(properties)}' property on the '{nameof(ServiceBusConnectionStringProperties)}' type");
            
            entityPathProperty?.SetValue(properties, entityPath, internalScope, binder: null, index: null, culture: null);
        }

        private static string GetConnectionString(ServiceBusConnectionStringProperties properties)
        {
            const BindingFlags internalScope = BindingFlags.NonPublic | BindingFlags.Instance;
            Type propertiesType = properties.GetType();
            
            const string connectionStringMethodName = "ToConnectionString";
            MethodInfo connectionStringMethod = propertiesType.GetMethod(connectionStringMethodName, internalScope);
            Guard.NotNull(connectionStringMethod, nameof(properties), 
                $"Requires a '{connectionStringMethodName}' method on the '{nameof(ServiceBusConnectionStringProperties)}'");

            var connectionString = connectionStringMethod?.Invoke(properties, new object[0]) as string;
            if (string.IsNullOrWhiteSpace(connectionStringMethodName))
            {
                throw new InvalidOperationException(
                    "Could not determine the Azure Service Bus namespace-scoped connection string from the given connection string properties");
            }
            
            return connectionString;
        }
    }
}
