from enum import Enum
from typing import Any
import io
import struct

from resonite.slot import Slot
from resonite.user import User
from resonite.userroot import UserRoot


class SimpleType(Enum):
    UNKNOWN = 0
    BOOL = 1
    BOOL2 = 2
    BOOL3 = 3
    BOOL4 = 4
    INT = 5
    INT2 = 6
    INT3 = 7
    INT4 = 8
    UINT = 9
    UINT2 = 10
    UINT3 = 11
    UINT4 = 12
    LONG = 13
    LONG2 = 14
    LONG3 = 15
    LONG4 = 16
    ULONG = 17
    ULONG2 = 18
    ULONG3 = 19
    ULONG4 = 20
    FLOAT = 21
    FLOAT2 = 22
    FLOAT3 = 23
    FLOAT4 = 24
    FLOATQ = 25
    DOUBLE = 26
    DOUBLE2 = 27
    DOUBLE3 = 28
    DOUBLE4 = 29
    DOUBLEQ = 30
    STRING = 31
    COLOR = 32
    COLORX = 33
    REFID = 34
    SLOT = 35
    USER = 36
    USERROOT = 37


def deserialize(data: bytes) -> Any:
    f = io.BytesIO(data)
    simple_type = SimpleType(struct.unpack("<i", f.read(4))[0])
    if simple_type == SimpleType.UNKNOWN:
        return None
    if simple_type == SimpleType.BOOL:
        return struct.unpack("<i", f.read(4))[0] != 0
    if simple_type == SimpleType.BOOL2:
        return (struct.unpack("<i", f.read(4))[0] != 0,
                struct.unpack("<i", f.read(4))[0] != 0)
    if simple_type == SimpleType.BOOL3:
        return (struct.unpack("<i", f.read(4))[0] != 0,
                struct.unpack("<i", f.read(4))[0] != 0,
                struct.unpack("<i", f.read(4))[0] != 0)
    if simple_type == SimpleType.BOOL4:
        return (struct.unpack("<i", f.read(4))[0] != 0,
                struct.unpack("<i", f.read(4))[0] != 0,
                struct.unpack("<i", f.read(4))[0] != 0,
                struct.unpack("<i", f.read(4))[0] != 0)

    if simple_type == SimpleType.INT:
        return struct.unpack("<i", f.read(4))[0]
    if simple_type == SimpleType.INT2:
        return (struct.unpack("<i", f.read(4))[0],
                struct.unpack("<i", f.read(4))[0])
    if simple_type == SimpleType.INT3:
        return (struct.unpack("<i", f.read(4))[0],
                struct.unpack("<i", f.read(4))[0],
                struct.unpack("<i", f.read(4))[0])
    if simple_type == SimpleType.INT4:
        return (struct.unpack("<i", f.read(4))[0],
                struct.unpack("<i", f.read(4))[0],
                struct.unpack("<i", f.read(4))[0],
                struct.unpack("<i", f.read(4))[0])

    if simple_type == SimpleType.UINT:
        return struct.unpack("<I", f.read(4))[0]
    if simple_type == SimpleType.UINT2:
        return (struct.unpack("<I", f.read(4))[0],
                struct.unpack("<I", f.read(4))[0])
    if simple_type == SimpleType.UINT3:
        return (struct.unpack("<I", f.read(4))[0],
                struct.unpack("<I", f.read(4))[0],
                struct.unpack("<I", f.read(4))[0])
    if simple_type == SimpleType.UINT4:
        return (struct.unpack("<I", f.read(4))[0],
                struct.unpack("<I", f.read(4))[0],
                struct.unpack("<I", f.read(4))[0],
                struct.unpack("<I", f.read(4))[0])

    if simple_type == SimpleType.LONG:
        return struct.unpack("<q", f.read(8))[0]
    if simple_type == SimpleType.LONG2:
        return (struct.unpack("<q", f.read(8))[0],
                struct.unpack("<q", f.read(8))[0])
    if simple_type == SimpleType.LONG3:
        return (struct.unpack("<q", f.read(8))[0],
                struct.unpack("<q", f.read(8))[0],
                struct.unpack("<q", f.read(8))[0])
    if simple_type == SimpleType.LONG4:
        return (struct.unpack("<q", f.read(8))[0],
                struct.unpack("<q", f.read(8))[0],
                struct.unpack("<q", f.read(8))[0],
                struct.unpack("<q", f.read(8))[0])

    if simple_type == SimpleType.ULONG:
        return struct.unpack("<Q", f.read(8))[0]
    if simple_type == SimpleType.ULONG2:
        return (struct.unpack("<Q", f.read(8))[0],
                struct.unpack("<Q", f.read(8))[0])
    if simple_type == SimpleType.ULONG3:
        return (struct.unpack("<Q", f.read(8))[0],
                struct.unpack("<Q", f.read(8))[0],
                struct.unpack("<Q", f.read(8))[0])
    if simple_type == SimpleType.ULONG4:
        return (struct.unpack("<Q", f.read(8))[0],
                struct.unpack("<Q", f.read(8))[0],
                struct.unpack("<Q", f.read(8))[0],
                struct.unpack("<Q", f.read(8))[0])

    if simple_type == SimpleType.FLOAT:
        return struct.unpack("<f", f.read(4))[0]
    if simple_type == SimpleType.FLOAT2:
        return (struct.unpack("<f", f.read(4))[0],
                struct.unpack("<f", f.read(4))[0])
    if simple_type == SimpleType.FLOAT3:
        return (struct.unpack("<f", f.read(4))[0],
                struct.unpack("<f", f.read(4))[0],
                struct.unpack("<f", f.read(4))[0])
    if simple_type == SimpleType.FLOAT4 or simple_type == SimpleType.FLOATQ:
        return (struct.unpack("<f", f.read(4))[0],
                struct.unpack("<f", f.read(4))[0],
                struct.unpack("<f", f.read(4))[0],
                struct.unpack("<f", f.read(4))[0])

    if simple_type == SimpleType.DOUBLE:
        return struct.unpack("<d", f.read(8))[0]
    if simple_type == SimpleType.DOUBLE2:
        return (struct.unpack("<d", f.read(8))[0],
                struct.unpack("<d", f.read(8))[0])
    if simple_type == SimpleType.DOUBLE3:
        return (struct.unpack("<d", f.read(8))[0],
                struct.unpack("<d", f.read(8))[0],
                struct.unpack("<d", f.read(8))[0])
    if simple_type == SimpleType.DOUBLE4 or simple_type == SimpleType.DOUBLEQ:
        return (struct.unpack("<d", f.read(8))[0],
                struct.unpack("<d", f.read(8))[0],
                struct.unpack("<d", f.read(8))[0],
                struct.unpack("<d", f.read(8))[0])

    if simple_type == SimpleType.STRING:
        count = struct.unpack("<i", f.read(4))[0]
        return f.read(count).decode("utf-8")

    if simple_type == SimpleType.COLOR or simple_type == SimpleType.COLORX:
        return (struct.unpack("<f", f.read(4))[0],
                struct.unpack("<f", f.read(4))[0],
                struct.unpack("<f", f.read(4))[0],
                struct.unpack("<f", f.read(4))[0])

    if simple_type == SimpleType.REFID:
        return struct.unpack("<Q", f.read(8))[0]

    if simple_type == SimpleType.SLOT:
        return Slot(struct.unpack("<Q", f.read(8))[0])

    if simple_type == SimpleType.USER:
        return User(struct.unpack("<Q", f.read(8))[0])

    if simple_type == SimpleType.USERROOT:
        return UserRoot(struct.unpack("<Q", f.read(8))[0])

    return None
