"""Machine interface."""

from __future__ import annotations  # For PEP563 - postponed evaluation of annotations

import abc
import dataclasses
from typing import TYPE_CHECKING, Callable, Type

from dergwasm.interpreter import binary

if TYPE_CHECKING:
    import module_instance
    import values
    import insn


@dataclasses.dataclass
class FuncInstance:
    """The base type for FuncInstances."""
    functype: binary.FuncType


@dataclasses.dataclass
class ModuleFuncInstance(FuncInstance):
    """A function instance in a module."""

    module: module_instance.ModuleInstance
    local_vars: list[values.ValueType]
    body: list[insn.Instruction]


@dataclasses.dataclass
class HostFuncInstance(FuncInstance):
    """A function instance on the host."""

    hostfunc: Callable


# TODO: Add elements.
@dataclasses.dataclass
class TableInstance(binary.Table):
    """A table instance."""


@dataclasses.dataclass
class GlobalInstance(binary.Global):
    """A global instance."""

    value: values.Value


class Machine(abc.ABC):
    """Abstract machine interface."""

    @abc.abstractmethod
    def push(self, value: values.StackValue) -> None:
        """Pushes a value onto the stack."""

    @abc.abstractmethod
    def pop(self) -> values.StackValue:
        """Pops a value off the stack."""

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
        """Adds a function to the machine and returns its index."""

    @abc.abstractmethod
    def get_func(self, funcidx: int) -> FuncInstance:
        """Returns the func at the given index."""

    @abc.abstractmethod
    def invoke_func(self, funcidx: int) -> None:
        """Invokes the func at the given index (address).

        The function must be a module function.
        """

    # Hmmmmmm.
    # @abc.abstractmethod
    # def _exit_insn_seq_with_label(self) -> values.Label:
    #     """Exits the current instruction sequence and returns the label."""

    def add_hostfunc(self, hostfunc: Callable) -> int:
        """Adds a host function to the machine and returns its index."""

    @abc.abstractmethod
    def add_table(self, table: TableInstance) -> int:
        """Adds a table to the machine and returns its index."""

    @abc.abstractmethod
    def get_table(self, tableidx: int) -> TableInstance:
        """Returns the table at the given index."""

    @abc.abstractmethod
    def add_mem(self, mem: bytearray) -> int:
        """Adds a memory to the machine and returns its index."""

    @abc.abstractmethod
    def get_mem(self, memidx: int) -> bytearray:
        """Returns the memory at the given index."""

    @abc.abstractmethod
    def add_global(self, global_: GlobalInstance) -> int:
        """Adds a global var to the machine and returns its index."""

    @abc.abstractmethod
    def set_global(self, globalidx: int, value: values.Value) -> None:
        """Sets the value of the global at the given index."""

    @abc.abstractmethod
    def get_global(self, globalidx: int) -> GlobalInstance:
        """Returns the global at the given index."""

    @abc.abstractmethod
    def add_data(self, data: bytearray) -> int:
        """Adds data to the machine and returns its index."""

    @abc.abstractmethod
    def get_data(self, dataidx: int) -> bytearray:
        """Returns the data at the given index."""

    @abc.abstractmethod
    def get_nth_value_of_type(
        self, n: int, value_type: Type[values.StackValue]
    ) -> values.StackValue:
        """Gets the n-th value of the given type from the top of the stack (0-based)."""
