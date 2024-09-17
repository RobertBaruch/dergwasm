﻿using System;

namespace Derg
{
    public static class TypeExtensions
    {
        // See https://stackoverflow.com/questions/982487/testing-if-object-is-of-generic-type-in-c-sharp
        public static bool IsOfGenericType(this Type typeToCheck, Type genericType)
        {
            Type concreteType;
            return typeToCheck.IsOfGenericType(genericType, out concreteType);
        }

        public static bool IsOfGenericType(
            this Type typeToCheck,
            Type genericType,
            out Type concreteGenericType
        )
        {
            while (true)
            {
                concreteGenericType = null;

                if (genericType == null)
                    throw new ArgumentNullException(nameof(genericType));

                if (!genericType.IsGenericTypeDefinition)
                    throw new ArgumentException(
                        "The definition needs to be a GenericTypeDefinition",
                        nameof(genericType)
                    );

                if (typeToCheck == null || typeToCheck == typeof(object))
                    return false;

                if (typeToCheck == genericType)
                {
                    concreteGenericType = typeToCheck;
                    return true;
                }

                if (
                    (
                        typeToCheck.IsGenericType
                            ? typeToCheck.GetGenericTypeDefinition()
                            : typeToCheck
                    ) == genericType
                )
                {
                    concreteGenericType = typeToCheck;
                    return true;
                }

                if (genericType.IsInterface)
                    foreach (var i in typeToCheck.GetInterfaces())
                        if (i.IsOfGenericType(genericType, out concreteGenericType))
                            return true;

                typeToCheck = typeToCheck.BaseType;
            }
        }
    }
}
