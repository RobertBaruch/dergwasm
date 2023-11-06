"""Values!"""

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

import dataclasses
import enum
from typing import TYPE_CHECKING, cast

if TYPE_CHECKING:
    from dergwasm.interpreter import module_instance


@enum.unique
class ValueType(enum.Enum):
    """Value types."""

    I32 = 0x7F  # value = int
    I64 = 0x7E  # value = int
    F32 = 0x7D  # value = float
    F64 = 0x7C  # value = float
    V128 = 0x7B  # value = int
    # TODO: How does this interact with RefValType?
    # FUNCREF is any reference to a function instance in the shared store, regardless
    # of its function type.
    FUNCREF = 0x70  # value = int (funcaddr) or None (null)
    # EXTERNREF is any reference to an object owned by the embedder.
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
            return Value(value_type, None)
        if value_type == ValueType.EXTERNREF:
            return Value(value_type, None)
        raise ValueError(f"Unknown value type {value_type}")


@dataclasses.dataclass
class Value(StackValue):
    """A value on the stack.

    This is one of:
    * int (i32, i64, v128)
    * int (FUNC reference = funcaddr in the store)
    * float (f32, f64)
    * None (null FUNC or EXTERNREF reference)
    * RefVal (EXTERNREF reference)
    """

    value_type: ValueType
    value: int | float | None | RefVal

    def intval(self) -> int:
        """Returns the value's int value. Must be an int."""
        assert isinstance(self.value, int)
        return cast(int, self.value)


@dataclasses.dataclass
class Frame(StackValue):
    """A value on the stack that is a frame."""

    arity: int
    local_vars: list[Value]
    module: module_instance.ModuleInstance
    # The current PC.
    pc: int
    # The previous frame, if any
    prev_frame: Frame | None = None


@dataclasses.dataclass
class Label(StackValue):
    """A value on the stack that is a label."""

    arity: int
    continuation: int  # offset into the function's body to run next.


@dataclasses.dataclass
class Limits:
    """Limits on a table or memory."""

    min: int
    max: int | None = None

    def is_valid(self, value: int) -> bool:
        """Returns True if the value is valid for this limit."""
        return (
            self.min <= value <= self.max and self.max >= self.min
            if self.max is not None
            else self.min <= value
        )
