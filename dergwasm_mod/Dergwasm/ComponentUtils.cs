using System;
using System.Reflection;
using FrooxEngine;

namespace Derg
{
    public static class ComponentUtils
    {
        public static object GetFieldValue(Component component, string fieldName)
        {
            Type componentType = component.GetType();
            PropertyInfo propertyInfo = componentType.GetProperty(fieldName);
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(component);
            }

            FieldInfo fieldInfo = componentType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public
            );
            if (fieldInfo == null)
                return null;
            object value = fieldInfo.GetValue(component);

            if (value.GetType().IsOfGenericType(typeof(SyncField<>)))
            {
                // If the field is a SyncField, we need to get the value of the Value property.
                return value.GetType().GetProperty("Value").GetValue(value);
            }
            return null;
        }
    }
}
