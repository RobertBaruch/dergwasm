using System;
using System.Reflection;
using Elements.Assets;
using FrooxEngine;

namespace Derg
{
    // Utilities for working with the fields of a Component.
    //
    // Component fields can be:
    // * Sync (field for a value)
    // * SyncRef (field for a RefID; this includes component fields)
    // * SyncDelegate (field for a WorldDelegate)
    //
    // All of these are SyncFields.
    //
    // SyncList is NOT a SyncField.
    public static class ComponentUtils
    {
        public static object GetFieldValue(object component, string fieldName)
        {
            if (component == null)
                return null;
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

            if (value.GetType().IsOfGenericType(typeof(Sync<>)))
            {
                // If the field is a Sync, we need to get the value of the Value property.
                return value.GetType().GetProperty("Value").GetValue(value);
            }
            if (value.GetType().IsOfGenericType(typeof(SyncRef<>)))
            {
                // If the field is a SyncRef, we need to get the value of the Target property.
                return value.GetType().GetProperty("Target").GetValue(value);
            }
            return null;
        }

        public static bool SetFieldValue(object component, string fieldName, object value)
        {
            if (component == null)
                return false;
            Type componentType = component.GetType();
            PropertyInfo propertyInfo = componentType.GetProperty(fieldName);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(component, value);
                return false;
            }

            FieldInfo fieldInfo = componentType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public
            );
            if (fieldInfo == null)
                return false;
            object syncValue = fieldInfo.GetValue(component);

            if (syncValue.GetType().IsOfGenericType(typeof(Sync<>)))
            {
                syncValue.GetType().GetProperty("Value").SetValue(syncValue, value);
                return true;
            }
            if (syncValue.GetType().IsOfGenericType(typeof(SyncRef<>)))
            {
                // If the field is a SyncRef, we need to get the value of the Target property.
                syncValue.GetType().GetProperty("Target").SetValue(syncValue, value);
                return true;
            }
            return false;
        }
    }
}
