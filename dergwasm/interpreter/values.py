"""Values!"""

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

import dataclasses
import enum
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from dergwasm.interpreter import module_instance


@enum.unique
class ValueType(enum.Enum):
    """Value types."""
    I32 = 0x7F
    I64 = 0x7E
    F32 = 0x7D
    F64 = 0x7C
    V128 = 0x7B
    # TODO: How does this interact with RefValType?
    FUNCREF = 0x70
    EXTERNREF = 0x6F

    def __repr__(self) -> str:
        return self.name


@enum.unique
class RefValType(enum.Enum):
    """External value types.

    An external value is the runtime representation of an entity that can be
    imported or exported. It is an address denoting either a function instance, table
    instance, memory instance, or global instances in the shared store.
    """

    EXTERN_FUNC = 0x00
    EXTERN_TABLE = 0x01
    EXTERN_MEMORY = 0x02
    EXTERN_GLOBAL = 0x03
    FUNC = 0x04

    def __repr__(self) -> str:
        return self.name


@dataclasses.dataclass
class RefVal:
    """A reference value.

    If the addr is None, then the reference is null.
    """
    val_type: RefValType
    addr: int | None

    def __repr__(self) -> str:
        return f"ref {self.val_type} {self.addr}"


class StackValue:
    """Base class for a value on the stack."""

    @staticmethod
    def default(value_type: ValueType) -> Value:
        """Returns the default value for the given value type."""
        if value_type in (ValueType.I32, ValueType.I64):
            return Value(value_type, 0)
        if value_type in (ValueType.F32, ValueType.F64):
            return Value(value_type, 0.0)
        if value_type == ValueType.FUNCREF:
            return Value(value_type, RefVal(RefValType.FUNC, None))
        if value_type == ValueType.EXTERNREF:
            return Value(value_type, RefVal(RefValType.EXTERN_FUNC, None))
        raise ValueError(f"Unknown value type {value_type}")


@dataclasses.dataclass
class Value(StackValue):
    """A value on the stack: either an int, a float, or a reference."""

    value_type: ValueType
    value: int | float | RefVal


@dataclasses.dataclass
class Frame(StackValue):
    """A value on the stack that is a frame."""

    arity: int
    local_vars: list[Value]
    module: module_instance.ModuleInstance
    # The current PC.
    pc: int


@dataclasses.dataclass
class Label(StackValue):
    """A value on the stack that is a label."""

    arity: int
    continuation: int  # offset into the function's body to run next.
