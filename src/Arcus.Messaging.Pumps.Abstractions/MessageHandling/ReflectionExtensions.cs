using System;
using System.Reflection;
using GuardNet;

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
        /// <typeparam name="TValue">The type of the field value to expect.</typeparam>
        /// <param name="instance">The instance to get the field from.</param>
        /// <param name="fieldName">The name of the field on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the field is declared on the <paramref name="instance"/>.</param>
        internal static TValue GetRequiredFieldValue<TValue>(this object instance, string fieldName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance) 
            where TValue : class
        {
            Guard.NotNull(instance, nameof(instance), $"Requires an instance object to get the field '{fieldName}'");
            
            object fieldValue = GetRequiredFieldValue(instance, fieldName, bindingFlags);
            if (fieldValue is TValue typedFieldValue)
            {
                return typedFieldValue;
            }

            throw new InvalidCastException(
                $"Cannot cast '{fieldValue.GetType().Name}' to type '{typeof(TValue).Name}' while getting field '{fieldName}' on instance '{instance.GetType().Name}'");
        }

        /// <summary>
        /// Gets the value of the field of the current <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance to get the field from.</param>
        /// <param name="fieldName">The name of the field on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the field is declared on the <paramref name="instance"/>.</param>
        internal static object GetRequiredFieldValue(this object instance, string fieldName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            Guard.NotNull(instance, nameof(instance), $"Requires a instance object to get the field '{fieldName}'");
            
            object fieldValue = GetFieldValue(instance, fieldName, bindingFlags);
            if (fieldValue is null)
            {
                throw new ValueMissingException($"There's no value for the field '{fieldName}' on instance '{instance.GetType().Name}'");
            }

            return fieldValue;
        }

        /// <summary>
        /// Gets the value of the field of the current <paramref name="instance"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the field value to expect.</typeparam>
        /// <param name="instance">The instance to get the field from.</param>
        /// <param name="fieldName">The name of the field on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the field is declared on the <paramref name="instance"/>.</param>
        internal static TValue GetFieldValue<TValue>(this object instance, string fieldName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
            where TValue : class
        {
            object fieldValue = GetFieldValue(instance, fieldName, bindingFlags);
            if (fieldValue is null)
            {
                return null;
            }

            if (fieldValue is TValue typedFieldValue)
            {
                return typedFieldValue;
            }

            throw new InvalidCastException(
                $"Cannot cast '{fieldValue.GetType().Name}' to type '{typeof(TValue).Name}' while getting field '{fieldName}' on instance '{instance.GetType().Name}'");
        }

        /// <summary>
        /// Gets the value of the field of the current <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance to get the field from.</param>
        /// <param name="fieldName">The name of the field on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the field is declared on the <paramref name="instance"/>.</param>
        internal static object GetFieldValue(this object instance, string fieldName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            Guard.NotNull(instance, nameof(instance), $"Requires a instance object to get the field '{fieldName}'");
            Type instanceType = instance.GetType();
            
            FieldInfo fieldInfo = instanceType.GetField(fieldName, bindingFlags);
            if (fieldInfo is null)
            {
                throw new TypeNotFoundException($"Cannot find field '{fieldName}' on instance '{instanceType.Name}'");
            }

            object fieldValue = fieldInfo.GetValue(instance);
            return fieldValue;
        }

        /// <summary>
        /// Gets the value of the property of the current <paramref name="instance"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value of the property to expect.</typeparam>
        /// <param name="instance">The instance to get the property from.</param>
        /// <param name="propertyName">The name of the property on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the property is declared  on the <paramref name="instance"/>.</param>
        internal static TValue GetRequiredPropertyValue<TValue>(this object instance, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
            where TValue : class
        {
            Guard.NotNull(instance, nameof(instance), $"Requires a instance object to get the property '{propertyName}'");

            object propertyValue = GetRequiredPropertyValue(instance, propertyName, bindingFlags);
            if (propertyValue is TValue typedPropertyValue)
            {
                return typedPropertyValue;
            }

            throw new InvalidCastException(
                $"Cannot cast '{propertyName.GetType().Name}' to type '{typeof(TValue).Name}' while getting property '{propertyName}' on instance '{instance.GetType().Name}'");
        }

        /// <summary>
        /// Gets the value of the property of the current <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance to get the property from.</param>
        /// <param name="propertyName">The name of the property on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the property is declared  on the <paramref name="instance"/>.</param>
        internal static object GetRequiredPropertyValue(this object instance, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            Guard.NotNull(instance, nameof(instance), $"Requires a instance object to get the property '{propertyName}'");

            object propertyValue = GetPropertyValue(instance, propertyName, bindingFlags);
            if (propertyValue is null)
            {
                Type instanceType = instance.GetType();
                throw new ValueMissingException($"There's no value for the property '{propertyName}' on instance '{instanceType.Name}'");
            }

            return propertyValue;
        }

        /// <summary>
        /// Gets the value of the property of the current <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance to get the property from.</param>
        /// <param name="propertyName">The name of the property on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the property is declared  on the <paramref name="instance"/>.</param>
        internal static TValue GetPropertyValue<TValue>(this object instance, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            Guard.NotNull(instance, nameof(instance), $"Requires a instance object to get the property '{propertyName}'");

            object propertyValue = GetPropertyValue(instance, propertyName, bindingFlags);
            if (propertyValue is null)
            {
                return default;
            }
            
            if (propertyValue is TValue typedPropertyValue)
            {
                return typedPropertyValue;
            }

            throw new InvalidCastException(
                $"Cannot cast '{propertyValue.GetType().Name}' to type '{typeof(TValue).Name}' while getting property '{propertyName}' on instance '{instance.GetType().Name}'");
        }

        /// <summary>
        /// Gets the value of the property of the current <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance to get the property from.</param>
        /// <param name="propertyName">The name of the property on the <paramref name="instance"/>.</param>
        /// <param name="bindingFlags">The way the property is declared  on the <paramref name="instance"/>.</param>
        internal static object GetPropertyValue(this object instance, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            Guard.NotNull(instance, nameof(instance), $"Requires a instance object to get the property '{propertyName}'");
            Type instanceType = instance.GetType();
            
            PropertyInfo propertyInfo = instanceType.GetProperty(propertyName, bindingFlags);
            if (propertyInfo is null)
            {
                throw new TypeNotFoundException($"Cannot find property '{propertyName}' on instance '{instanceType.Name}'");
            }

            object propertyValue = propertyInfo.GetValue(instance);
            return propertyValue;
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
            Guard.NotNull(instance, nameof(instance), $"Requires a instance object to get the indexed property '{propertyName}'");

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
            Guard.NotNull(instance, nameof(instance), $"Requires a instance object to invoke the method '{methodName}'");

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

            object returnValue = methodInfo.Invoke(instance, parameters);
            return returnValue;
        }
    }
}