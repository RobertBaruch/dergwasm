using Elements.Core;
using FrooxEngine;
using System.IO;
using System.Text;

namespace Derg
{
    public static class SimpleSerialization
    {
        public static class SimpleType
        {
            public const int Unknown = 0;
            public const int Bool = 1;
            public const int Bool2 = 2;
            public const int Bool3 = 3;
            public const int Bool4 = 4;
            public const int Int = 5;
            public const int Int2 = 6;
            public const int Int3 = 7;
            public const int Int4 = 8;
            public const int UInt = 9;
            public const int UInt2 = 10;
            public const int UInt3 = 11;
            public const int UInt4 = 12;
            public const int Long = 13;
            public const int Long2 = 14;
            public const int Long3 = 15;
            public const int Long4 = 16;
            public const int ULong = 17;
            public const int ULong2 = 18;
            public const int ULong3 = 19;
            public const int ULong4 = 20;
            public const int Float = 21;
            public const int Float2 = 22;
            public const int Float3 = 23;
            public const int Float4 = 24;
            public const int FloatQ = 25;
            public const int Double = 26;
            public const int Double2 = 27;
            public const int Double3 = 28;
            public const int Double4 = 29;
            public const int DoubleQ = 30;
            public const int String = 31;
            public const int Color = 32;
            public const int ColorX = 33;
            public const int RefID = 34;
            public const int Slot = 35;
            public const int User = 36;
            public const int UserRoot = 37;
        }

        // Serializes a "simple" value. A simple value is one of these:
        //
        // * bool, bool2, bool3, bool4
        // * int, int2, int3, int4
        // * uint, uint2, uint3, uint4
        // * long, long2, long3, long4
        // * ulong, ulong2, ulong3, ulong4
        // * float, float2, float3, float4, floatQ
        // * double, double2, double3, double4, doubleQ
        // * string
        // * color, colorX
        // * RefID
        // * Slot, User, UserRoot (represented as their RefID)
        //
        // Allocates enough space for the serialized value in the machine heap, and
        // returns the pointer to memory allocated. The `len` parameter is set to the
        // amount of memory allocated.
        public static int Serialize(
            Machine machine,
            EmscriptenEnv emscriptenEnv,
            Frame frame,
            object value,
            out int len
        )
        {
            int dataPtr;
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            switch (value)
            {
                case bool b:
                    writer.Write(SimpleType.Bool);
                    writer.Write(b ? 1 : 0);
                    break;

                case bool2 b2:
                    writer.Write(SimpleType.Bool2);
                    writer.Write(b2.x ? 1 : 0);
                    writer.Write(b2.y ? 1 : 0);
                    break;

                case bool3 b3:
                    writer.Write(SimpleType.Bool3);
                    writer.Write(b3.x ? 1 : 0);
                    writer.Write(b3.y ? 1 : 0);
                    writer.Write(b3.z ? 1 : 0);
                    break;

                case bool4 b4:
                    writer.Write(SimpleType.Bool4);
                    writer.Write(b4.x ? 1 : 0);
                    writer.Write(b4.y ? 1 : 0);
                    writer.Write(b4.z ? 1 : 0);
                    writer.Write(b4.w ? 1 : 0);
                    break;

                case int i:
                    writer.Write(SimpleType.Int);
                    writer.Write(i);
                    break;

                case int2 i2:
                    writer.Write(SimpleType.Int2);
                    writer.Write(i2.x);
                    writer.Write(i2.y);
                    break;

                case int3 i3:
                    writer.Write(SimpleType.Int3);
                    writer.Write(i3.x);
                    writer.Write(i3.y);
                    writer.Write(i3.z);
                    break;

                case int4 i4:
                    writer.Write(SimpleType.Int4);
                    writer.Write(i4.x);
                    writer.Write(i4.y);
                    writer.Write(i4.z);
                    writer.Write(i4.w);
                    break;

                case uint ui:
                    writer.Write(SimpleType.UInt);
                    writer.Write(ui);
                    break;

                case uint2 ui2:
                    writer.Write(SimpleType.UInt2);
                    writer.Write(ui2.x);
                    writer.Write(ui2.y);
                    break;

                case uint3 ui3:
                    writer.Write(SimpleType.UInt3);
                    writer.Write(ui3.x);
                    writer.Write(ui3.y);
                    writer.Write(ui3.z);
                    break;

                case uint4 ui4:
                    writer.Write(SimpleType.UInt4);
                    writer.Write(ui4.x);
                    writer.Write(ui4.y);
                    writer.Write(ui4.z);
                    writer.Write(ui4.w);
                    break;

                case long l:
                    writer.Write(SimpleType.Long);
                    writer.Write(l);
                    break;

                case long2 l2:
                    writer.Write(SimpleType.Long2);
                    writer.Write(l2.x);
                    writer.Write(l2.y);
                    break;

                case long3 l3:
                    writer.Write(SimpleType.Long3);
                    writer.Write(l3.x);
                    writer.Write(l3.y);
                    writer.Write(l3.z);
                    break;

                case long4 l4:
                    writer.Write(SimpleType.Long4);
                    writer.Write(l4.x);
                    writer.Write(l4.y);
                    writer.Write(l4.z);
                    writer.Write(l4.w);
                    break;

                case ulong ul:
                    writer.Write(SimpleType.ULong);
                    writer.Write(ul);
                    break;

                case ulong2 ul2:
                    writer.Write(SimpleType.ULong2);
                    writer.Write(ul2.x);
                    writer.Write(ul2.y);
                    break;

                case ulong3 ul3:
                    writer.Write(SimpleType.ULong3);
                    writer.Write(ul3.x);
                    writer.Write(ul3.y);
                    writer.Write(ul3.z);
                    break;

                case ulong4 ul4:
                    writer.Write(SimpleType.ULong4);
                    writer.Write(ul4.x);
                    writer.Write(ul4.y);
                    writer.Write(ul4.z);
                    writer.Write(ul4.w);
                    break;

                case float f:
                    writer.Write(SimpleType.Float);
                    writer.Write(f);
                    break;

                case float2 f2:
                    writer.Write(SimpleType.Float2);
                    writer.Write(f2.x);
                    writer.Write(f2.y);
                    break;

                case float3 f3:
                    writer.Write(SimpleType.Float3);
                    writer.Write(f3.x);
                    writer.Write(f3.y);
                    writer.Write(f3.z);
                    break;

                case float4 f4:
                    writer.Write(SimpleType.Float4);
                    writer.Write(f4.x);
                    writer.Write(f4.y);
                    writer.Write(f4.z);
                    writer.Write(f4.w);
                    break;

                case floatQ fq:
                    writer.Write(SimpleType.FloatQ);
                    writer.Write(fq.x);
                    writer.Write(fq.y);
                    writer.Write(fq.z);
                    writer.Write(fq.w);
                    break;

                case double d:
                    writer.Write(SimpleType.Double);
                    writer.Write(d);
                    break;

                case double2 d2:
                    writer.Write(SimpleType.Double2);
                    writer.Write(d2.x);
                    writer.Write(d2.y);
                    break;

                case double3 d3:
                    writer.Write(SimpleType.Double3);
                    writer.Write(d3.x);
                    writer.Write(d3.y);
                    writer.Write(d3.z);
                    break;

                case double4 d4:
                    writer.Write(SimpleType.Double4);
                    writer.Write(d4.x);
                    writer.Write(d4.y);
                    writer.Write(d4.z);
                    writer.Write(d4.w);
                    break;

                case doubleQ dq:
                    writer.Write(SimpleType.DoubleQ);
                    writer.Write(dq.x);
                    writer.Write(dq.y);
                    writer.Write(dq.z);
                    writer.Write(dq.w);
                    break;

                case string s:
                    writer.Write(SimpleType.String);
                    writer.Write(Encoding.UTF8.GetByteCount(s));
                    writer.Write(Encoding.UTF8.GetBytes(s));
                    break;

                case color c:
                    writer.Write(SimpleType.Color);
                    writer.Write(c.r);
                    writer.Write(c.g);
                    writer.Write(c.b);
                    writer.Write(c.a);
                    break;

                case colorX cx:
                    writer.Write(SimpleType.ColorX);
                    writer.Write(cx.r);
                    writer.Write(cx.g);
                    writer.Write(cx.b);
                    writer.Write(cx.a);
                    break;

                case RefID refID:
                    writer.Write(SimpleType.RefID);
                    writer.Write((ulong)refID);
                    break;

                case Slot slot:
                    writer.Write(SimpleType.Slot);
                    writer.Write((ulong)slot.ReferenceID);
                    break;

                case User user:
                    writer.Write(SimpleType.User);
                    writer.Write((ulong)user.ReferenceID);
                    break;

                case UserRoot userRoot:
                    writer.Write(SimpleType.UserRoot);
                    writer.Write((ulong)userRoot.ReferenceID);
                    break;

                default:
                    len = 0;
                    return 0;
            }

            dataPtr = emscriptenEnv.Malloc(frame, (int)stream.Length);
            len = (int)stream.Length;

            // There doesn't seem to be a way to directly copy the contents of a
            // MemoryStream to a byte array.
            MemoryStream memoryStream = new MemoryStream(machine.Memory0, dataPtr, len);
            stream.Position = 0;
            stream.CopyTo(memoryStream);
            return dataPtr;
        }
    }
}
