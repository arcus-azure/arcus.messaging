using System;
using System.Reflection;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling 
{
    /// <summary>
    /// Extensions on any object instance to provide a more stable approach when accessing types in a reflected manner.
    /// </summary>
    internal static class ReflectionExtensions
    {
        /// <summary>
        /// Gets the value of the field of the current <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance to get the field from.</param>
        /// <param name="fieldName">The name of the field on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the field is declared on the <paramref name="instance"/>.</param>
        internal static object GetFieldValue(this object instance, string fieldName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            Type instanceType = instance.GetType();
            FieldInfo fieldInfo = instanceType.GetField(fieldName, bindingFlags);

            if (fieldInfo is null)
            {
                throw new TypeNotFoundException($"Cannot find field '{fieldName}' on instance '{instanceType.Name}'");
            }

            return fieldInfo.GetValue(instance);
        }

        /// <summary>
        /// Gets the value of the property of the current <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance to get the property from.</param>
        /// <param name="propertyName">The name of the property on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the property is declared  on the <paramref name="instance"/>.</param>
        internal static object GetPropertyValue(this object instance, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            Type instanceType = instance.GetType();
            PropertyInfo propertyInfo = instanceType.GetProperty(propertyName, bindingFlags);
            
            if (propertyInfo is null)
            {
                throw new TypeNotFoundException($"Cannot find property '{propertyName}' on instance '{instanceType.Name}'");
            }

            return propertyInfo.GetValue(instance);
        }

        /// <summary>
        /// Sets the value of the index property of the current <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance to set the property on.</param>
        /// <param name="propertyName">The name of the property on the <paramref name="instance"/>.</param>
        /// <param name="index">The index value to select which value to set.</param>
        /// <param name="value">The new value to set on the property.</param>
        /// <param name="bindingFlags">The way the property is declared on the <paramref name="instance"/>.</param>
        internal static void SetIndexValue(this object instance, string propertyName, object index, object value, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            Type instanceType = instance.GetType();
            PropertyInfo propertyInfo = instanceType.GetProperty(propertyName, bindingFlags);
            
            if (propertyInfo is null)
            {
                throw new TypeNotFoundException($"Cannot find property '{propertyName}' on instance '{instanceType.Name}'");
            }

            propertyInfo.SetValue(instance, value, new[] { index });
        }

        /// <summary>
        /// Invokes a non-public instance method on the current <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance to invoke the method on.</param>
        /// <param name="methodName">The name of the method on the <paramref name="instance"/>.</param>
        /// <param name="parameters">The set of parameters to pass along the method.</param>
        /// <returns>
        ///     The return value of the invoked method.
        /// </returns>
        internal static object InvokeMethod(this object instance, string methodName, params object[] parameters)
        {
            return InvokeMethod(instance, methodName, BindingFlags.Instance | BindingFlags.NonPublic, parameters);
        }

        /// <summary>
        /// Invokes a method on the current <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance to invoke the method on.</param>
        /// <param name="methodName">The name of the method on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the method is declared on the <paramref name="instance"/>.</param>
        /// <param name="parameters">The set of parameters to pass along the method.</param>
        /// <returns>
        ///     The return value of the invoked method.
        /// </returns>
        internal static object InvokeMethod(this object instance, string methodName, BindingFlags bindingFlags, params object[] parameters)
        {
            Type instanceType = instance.GetType();
            MethodInfo methodInfo = instanceType.GetMethod(methodName, bindingFlags);

            if (methodInfo is null)
            {
                throw new TypeNotFoundException($"Cannot find method '{methodName}' on instance '{instanceType.Name}'");
            }

            return methodInfo.Invoke(instance, parameters);
        }
    }
}