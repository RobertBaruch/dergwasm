using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Derg.Mem
{
    public static class MarshalManager
    {
        // A map of type -> marshaller, pre-seeded with external types. Other types will use default marshallers.
        private static readonly ConcurrentDictionary<Type, Type> DefaultMarshallers = new ConcurrentDictionary<Type, Type>();

        static MarshalManager()
        {
            RegBlit<byte>();
            RegBlit<sbyte>();
            RegBlit<short>();
            RegBlit<ushort>();
            RegBlit<int>();
            RegBlit<uint>();
            RegBlit<long>();
            RegBlit<ulong>();
            RegBlit<float>();
            RegBlit<double>();
            RegMarshaller<string, TerminatedUtf8Marshaller>();
        }

        /// <summary>
        /// Registers a bitable type forcibly, this is mostly for System types.
        /// </summary>
        /// <typeparam name="TType">The type to be registered as bitable.</typeparam>
        private static void RegBlit<TType>() where TType : unmanaged
        {
            RegMarshaller<TType, BlitMarshaller<TType>>();
        }

        private static void RegMarshaller<TType, TMarshaller>() where TMarshaller : IMarshaller<TType>
        {
            DefaultMarshallers.TryAdd(typeof(TType), typeof(TMarshaller));
        }

        public static Type GetMarshallerFor(ParameterInfo parameterInfo)
        {
            var attrib = parameterInfo.GetCustomAttribute<MemMarshallerAttribute>();
            return attrib?.Marshaller ?? GetMarshallerFor(parameterInfo.ParameterType);
        }

        public static Type GetMarshallerFor(Type type)
        {
            return DefaultMarshallers.GetOrAdd(type, GetMarshallerForInternal);
        }

        private static Type GetMarshallerForInternal(Type type)
        {
            // The type specifies an explicit marshaller.
            var attrib = type.GetCustomAttribute<MemMarshallerAttribute>();
            if (attrib != null)
            {
                return attrib.Marshaller;
            }

            // It's bitable (probably), if a type implements StructLayout, but isn't bitable, something is wrong.
            if (type.StructLayoutAttribute?.Value != LayoutKind.Auto)
            {
                return typeof(BlitMarshaller<>).MakeGenericType(type);
            }

            throw new NotSupportedException($"Type {type} did not provide an explicit marshaller with the MemMarshaller attribute, and was not bitable.");
        }
    }
}
