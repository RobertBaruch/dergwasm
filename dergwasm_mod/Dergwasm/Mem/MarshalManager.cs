using System;

namespace Derg.Mem
{
    public static class MarshalManager<T>
    {
        public static readonly Type Marshaller = GetMarshallerType();

        private static Type GetMarshallerType() {
            throw new NotImplementedException();
        }
    }
}
