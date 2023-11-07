"""Machine interface."""

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

import abc
import dataclasses
from typing import TYPE_CHECKING, Callable, Type, TypeVar

from dergwasm.interpreter import binary

if TYPE_CHECKING:
    from dergwasm.interpreter import module_instance
    from dergwasm.interpreter import values
    from dergwasm.interpreter import insn


T = TypeVar("T")


@dataclasses.dataclass
class FuncInstance:
    """The base type runtime representation of a function."""

    functype: binary.FuncType


@dataclasses.dataclass
class ModuleFuncInstance(FuncInstance):
    """The runtime representation of a function in a module."""

    module: module_instance.ModuleInstance
    local_var_types: list[values.ValueType]
    body: list[insn.Instruction]


@dataclasses.dataclass
class HostFuncInstance(FuncInstance):
    """The runtime represetnation of a function on the host."""

    hostfunc: Callable


@dataclasses.dataclass
class TableInstance(binary.Table):
    """The runtime representation of a table."""

    refs: list[values.Value]


@dataclasses.dataclass
class MemInstance(binary.Mem):
    """The runtime representation of a memory."""

    data: bytearray


@dataclasses.dataclass
class GlobalInstance(binary.Global):
    """The runtime representation of a global variable."""

    value: values.Value


@dataclasses.dataclass
class ElementSegmentInstance(binary.ElementSegment):
    """The runtime representation of an element segment."""

    refs: list[values.Value] = dataclasses.field(default_factory=list)


class Machine(abc.ABC):
    """Abstract machine interface."""

    @abc.abstractmethod
    def get_max_allowed_memory_pages(self) -> int:
        """Gets the maximum memory pages you're allowed to have."""

    @abc.abstractmethod
    def push(self, value: values.StackValue) -> None:
        """Pushes a value onto the stack."""

    @abc.abstractmethod
    def pop(self) -> values.StackValue:
        """Pops a value off the stack."""

    @abc.abstractmethod
    def pop_value(self) -> values.Value:
        """Pops a value off the stack and asserts it's a Value."""

    @abc.abstractmethod
    def peek(self) -> values.StackValue:
        """Returns the value on the top of the stack."""

    @abc.abstractmethod
    def clear_stack(self) -> None:
        """Clears the stack."""

    @abc.abstractmethod
    def execute_seq(self, seq: list[insn.Instruction]) -> None:
        """Execute the instructions until RETURN or falling off end."""

    @abc.abstractmethod
    def execute_expr(self, expr: list[insn.Instruction]) -> None:
        """Execute the instructions until falling off end. Requires a frame."""

    @abc.abstractmethod
    def get_current_frame(self) -> values.Frame:
        """Returns the current frame."""

    @abc.abstractmethod
    def new_frame(self, frame: values.Frame) -> None:
        """Pushes the frame onto the stack and sets it as the current frame."""

    @abc.abstractmethod
    def pop_to_frame(self) -> None:
        """Pops from the stack up to and including frame, set that as current frame."""

    @abc.abstractmethod
    def add_func(self, func: FuncInstance) -> int:
        """Adds a function to the machine and returns its address."""

    @abc.abstractmethod
    def get_func(self, funcaddr: int) -> FuncInstance:
        """Returns the func at the given address."""

    @abc.abstractmethod
    def invoke_func(self, funcaddr: int) -> None:
        """Invokes the func at the given address.

        The function must be a module function.
        """

    @abc.abstractmethod
    def add_hostfunc(self, hostfunc: Callable) -> int:
        """Adds a host function to the machine and returns its address."""

    @abc.abstractmethod
    def add_table(self, table: TableInstance) -> int:
        """Adds a table to the machine and returns its address."""

    @abc.abstractmethod
    def get_table(self, tableaddr: int) -> TableInstance:
        """Returns the table at the given address."""

    @abc.abstractmethod
    def add_mem(self, mem: MemInstance) -> int:
        """Adds a memory to the machine and returns its address."""

    @abc.abstractmethod
    def get_mem(self, memaddr: int) -> MemInstance:
        """Returns the memory at the given address."""

    @abc.abstractmethod
    def get_mem_data(self, memaddr: int) -> bytearray:
        """Returns the memory at the given address."""

    @abc.abstractmethod
    def add_global(self, global_: GlobalInstance) -> int:
        """Adds a global var to the machine and returns its address."""

    @abc.abstractmethod
    def set_global(self, globaladdr: int, value: values.Value) -> None:
        """Sets the value of the global at the given address."""

    @abc.abstractmethod
    def get_global(self, globaladdr: int) -> GlobalInstance:
        """Returns the global at the given address."""

    @abc.abstractmethod
    def add_data(self, data: bytes) -> int:
        """Adds data to the machine and returns its address."""

    @abc.abstractmethod
    def get_data(self, dataaddr: int) -> bytes | None:
        """Returns the data at the given address."""

    @abc.abstractmethod
    def drop_data(self, dataaddr: int) -> None:
        """Drops the data at the given address."""

    @abc.abstractmethod
    def add_element(self, element: ElementSegmentInstance) -> int:
        """Adds an element segment to the machine and returns its address."""

    @abc.abstractmethod
    def get_element(self, elementaddr: int) -> ElementSegmentInstance | None:
        """Returns the element segment at the given address."""

    @abc.abstractmethod
    def drop_element(self, elementaddr: int) -> None:
        """Drops the element segment at the given address."""

    @abc.abstractmethod
    def get_nth_value_of_type(self, n: int, value_type: Type[T]) -> T:
        """Gets the n-th value of the given type from the top of the stack (0-based)."""

    @abc.abstractmethod
    def _debug_stack(self) -> None:
        """Prints the stack."""
