using System;
using System.Reflection;
using Elements.Core; // For UniLog
using FrooxEngine;

namespace Derg
{
    // Utilities for working primarily with the sync fields and properties of a
    // Component, but they'll work with any object. Uses reflection.
    //
    // Sync fields can be:
    // * Sync (field for a value)
    // * SyncRef (field for a RefID; this includes component fields)
    // * SyncDelegate (field for a WorldDelegate)
    // * SyncType (field for a System.Type)
    //
    // All of these are SyncFields.
    //
    // SyncList is NOT a SyncField.
    public static class ComponentUtils
    {
        // Gets the value of a field on a component. Returns true on success, false
        // if the component was null or if there was no property or field of the given
        // name.
        //
        // For properties, this gets the property value.
        // For Sync fields, this gets the Sync's value.
        // For SyncRef fields, this gets the SyncRef's Target (not its value).
        // For SyncDelegate fields, this gets the SyncDelegate's WorldDelegate.
        // For SyncType fields, this gets the SyncType's held System.Type.
        //
        // Note that the value may be null if not set.
        public static bool GetFieldValue(object component, string fieldName, out object value)
        {
            value = null;
            if (component == null)
                return false;
            Type componentType = component.GetType();
            PropertyInfo propertyInfo = componentType.GetProperty(fieldName);
            if (propertyInfo != null)
            {
                value = propertyInfo.GetValue(component);
                return true;
            }

            FieldInfo fieldInfo = componentType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public
            );
            if (fieldInfo == null)
                return false;
            value = fieldInfo.GetValue(component);

            if (
                fieldInfo.FieldType.IsOfGenericType(typeof(Sync<>))
                || fieldInfo.FieldType.IsOfGenericType(typeof(SyncDelegate<>))
                || fieldInfo.FieldType == typeof(SyncType)
            )
            {
                value = value.GetType().GetProperty("Value").GetValue(value);
                return true;
            }
            if (fieldInfo.FieldType.IsOfGenericType(typeof(SyncRef<>)))
            {
                value = value.GetType().GetProperty("Target").GetValue(value);
                return true;
            }
            return true;
        }

        // Sets the value of a field on a component. Returns true on success, false
        // if the component was null, there was no property or field of the given
        // name, or the property or field was not settable, or the value was of the
        // wrong type.
        //
        // For properties, this sets the property value.
        // For Sync fields, this sets the Sync's value.
        // For SyncRef fields, this sets the SyncRef's Target (not its value).
        // For SyncDelegate fields, this sets the SyncDelegate's WorldDelegate.
        // For SyncType fields, this sets the SyncType's held System.Type.
        public static bool SetFieldValue(object component, string fieldName, object value)
        {
            try
            {
                if (component == null)
                    return false;
                Type componentType = component.GetType();
                PropertyInfo propertyInfo = componentType.GetProperty(fieldName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(component, value);
                    return true;
                }

                FieldInfo fieldInfo = componentType.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public
                );
                if (fieldInfo == null)
                    return false;

                if (
                    fieldInfo.FieldType.IsOfGenericType(typeof(Sync<>))
                    || fieldInfo.FieldType.IsOfGenericType(typeof(SyncDelegate<>))
                    || fieldInfo.FieldType == typeof(SyncType)
                )
                {
                    object field = fieldInfo.GetValue(component);
                    field.GetType().GetProperty("Value").SetValue(field, value);
                    return true;
                }
                if (fieldInfo.FieldType.IsOfGenericType(typeof(SyncRef<>)))
                {
                    object field = fieldInfo.GetValue(component);
                    field.GetType().GetProperty("Target").SetValue(field, value);
                    return true;
                }
                fieldInfo.SetValue(component, value);
                return true;
            }
            catch (Exception e) // Wrong type
            {
                DergwasmMachine.Msg(
                    $"Failed to set field value '{fieldName}' on object of type {component.GetType()}: {e}"
                );
                return false;
            }
        }
    }
}
