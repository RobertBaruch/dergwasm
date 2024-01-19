using System;

namespace Derg.Mem
{
    /// <summary>
    /// Tags a type or parameter as using a specific marshaller.
    /// 
    /// Marshallers targeted with this type must implement <see cref="IMarshaller{T}"/> where T is the type being attributed,
    /// and the marshaller must have a default constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class MemMarshallerAttribute : Attribute
    {
        /// <summary>
        /// The marshaller to use in a particular context.
        /// </summary>
        public Type Marshaller { get; }

        public MemMarshallerAttribute(Type marshaller)
        {
            if (marshaller.FindInterfaces((t, _) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMarshaller<>), null) == null)
            {
                throw new ArgumentException("Marshaller must implement IMarshaller", nameof(marshaller));
            }
            Marshaller = marshaller;
        }
    }
}