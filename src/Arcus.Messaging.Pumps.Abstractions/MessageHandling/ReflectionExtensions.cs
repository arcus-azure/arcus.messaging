using System;
using System.Reflection;

namespace Arcus.Messaging.Pumps.Abstractions.MessageHandling 
{
    internal static class ReflectionExtensions
    {
        internal static object GetField(this object instance, string fieldName)
        {
            Type instanceType = instance.GetType();
            FieldInfo fieldInfo = instanceType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo is null)
            {
                throw new TypeNotFoundException($"Cannot find field '{fieldName}' on instance '{instanceType.Name}'");
            }

            return fieldInfo.GetValue(instance);
        }

        internal static object GetProperty(this object instance, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            Type instanceType = instance.GetType();
            PropertyInfo propertyInfo = instanceType.GetProperty(propertyName, bindingFlags);
            
            if (propertyInfo is null)
            {
                throw new TypeNotFoundException($"Cannot find property '{propertyName}' on instance '{instanceType.Name}'");
            }

            return propertyInfo.GetValue(instance);
        }

        internal static void SetProperty(this object instance, string propertyName, object value, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance, params object[] index)
        {
            Type instanceType = instance.GetType();
            PropertyInfo propertyInfo = instanceType.GetProperty(propertyName, bindingFlags);
            
            if (propertyInfo is null)
            {
                throw new TypeNotFoundException($"Cannot find property '{propertyName}' on instance '{instanceType.Name}'");
            }

            if (index.Length == 0)
            {
                propertyInfo.SetValue(instance, value);
            }
            else
            {
                propertyInfo.SetValue(instance, value, index);
            }
            
        }

        internal static object InvokeMethod(this object instance, string methodName, params object[] parameters)
        {
            return InvokeMethod(instance, methodName, BindingFlags.Instance | BindingFlags.NonPublic, parameters);
        }

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