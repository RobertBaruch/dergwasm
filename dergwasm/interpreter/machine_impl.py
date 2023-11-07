"""Concrete implementation of the machine interface."""

from typing import Callable, Type, cast, TypeVar

from dergwasm.interpreter import stack
from dergwasm.interpreter import machine
from dergwasm.interpreter import values
from dergwasm.interpreter.insn import Instruction, InstructionType
from dergwasm.interpreter import insn_eval


T = TypeVar("T")


class MachineImpl(machine.Machine):
    """The state of the machine."""

    current_frame: values.Frame | None
    stack_: stack.Stack
    funcs: list[machine.FuncInstance]
    tables: list[machine.TableInstance]
    mems: list[machine.MemInstance]
    global_vars: list[machine.GlobalInstance]
    datas: list[bytes | None]  # None is for dropped data
    # None is for dropped element segments
    element_segments: list[machine.ElementSegmentInstance | None]

    def __init__(self) -> None:
        self.current_frame = None
        self.stack_ = stack.Stack()
        self.funcs = []
        self.tables = []
        self.mems = []
        self.global_vars = []
        self.datas = []
        self.element_segments = []

    def get_max_allowed_memory_pages(self) -> int:
        # Here I'm just allowing 64k x 1k = 64M of memory. The WASM spec allows more,
        # 64k x 64k = 4G, but that's a lot of memory to allocate.
        return 1024

    def push(self, value: values.StackValue) -> None:
        self.stack_.push(value)

    def pop(self) -> values.StackValue:
        return self.stack_.pop()

    def pop_value(self) -> values.Value:
        val = self.stack_.pop()
        if not isinstance(val, values.Value):
            raise RuntimeError(f"Expected a value, got {val}")
        return val

    def peek(self) -> values.StackValue:
        return self.stack_.data[-1]

    def clear_stack(self) -> None:
        self.stack_.data = []

    def execute_seq(self, seq: list[Instruction]) -> None:
        """Execute the body of a function until RETURN or falling off end."""
        self.get_current_frame().pc = 0
        while self.get_current_frame().pc < len(seq):
            instruction = seq[self.get_current_frame().pc]
            if instruction.instruction_type == InstructionType.RETURN:
                break
            insn_eval.eval_insn(self, instruction)

        print("FELL OFF END, Stack before:")
        self._debug_stack()

        # Save any results.
        f = self.get_current_frame()
        n = f.arity
        results = [self.pop() for _ in range(n)]
        # Skip over everything up to and including the current frame. This skips over
        # all nesting labels.
        while not isinstance(self.peek(), values.Frame):
            self.pop()
        # Pop off the current frame.
        self.pop()
        # Restore the next frame as the current frame.
        self.current_frame = self.stack_.get_topmost_value_of_type(values.Frame)
        # Push the results back on the stack
        for v in reversed(results):
            self.push(v)

        print("FELL OFF END, Stack after:")
        self._debug_stack()

    def execute_expr(self, expr: list[Instruction]) -> None:
        """Execute the instructions until falling off end."""
        self.get_current_frame().pc = 0
        while self.get_current_frame().pc < len(expr):
            instruction = expr[self.get_current_frame().pc]
            if instruction.instruction_type == InstructionType.RETURN:
                raise RuntimeError(
                    "Unexpected RETURN instruction in an initialization expression"
                )
            insn_eval.eval_insn(self, instruction)

    def add_hostfunc(self, hostfunc: Callable) -> int:
        raise NotImplementedError()

    def get_current_frame(self) -> values.Frame:
        if self.current_frame is None:
            self.current_frame = cast(
                values.Frame, self.stack_.get_topmost_value_of_type(values.Frame)
            )
        return self.current_frame

    def new_frame(self, frame: values.Frame) -> None:
        frame.prev_frame = self.current_frame
        self.current_frame = frame
        self.push(frame)

    def pop_to_frame(self) -> None:
        frame = self.pop()
        while not isinstance(frame, values.Frame):
            frame = self.pop()
        self.current_frame = frame

    def add_func(self, func: machine.FuncInstance) -> int:
        self.funcs.append(func)
        return len(self.funcs) - 1

    def get_func(self, funcaddr: int) -> machine.FuncInstance:
        return self.funcs[funcaddr]

    def invoke_func(self, funcaddr: int) -> None:
        """Invokes a function, returning when the function ends/returns/traps."""
        f = self.funcs[funcaddr]
        if not isinstance(f, machine.ModuleFuncInstance):
            raise RuntimeError(f"Cannot invoke a non-module function: {f}")
        func_type = f.functype
        local_vars: list[values.Value] = [
            cast(values.Value, self.pop()) for _ in func_type.parameters
        ]
        local_vars.extend([values.StackValue.default(v) for v in f.local_var_types])
        self.new_frame(
            values.Frame(len(func_type.results), local_vars, f.module, funcaddr)
        )
        self.push(values.Label(len(func_type.results), len(f.body)))

        self.execute_seq(f.body)

    def add_table(self, table: machine.TableInstance) -> int:
        self.tables.append(table)
        return len(self.tables) - 1

    def get_table(self, tableaddr: int) -> machine.TableInstance:
        return self.tables[tableaddr]

    def add_mem(self, mem: machine.MemInstance) -> int:
        self.mems.append(mem)
        return len(self.mems) - 1

    def get_mem(self, memaddr: int) -> machine.MemInstance:
        return self.mems[memaddr]

    def get_mem_data(self, memaddr: int) -> bytearray:
        return self.mems[memaddr].data

    def add_global(self, global_: machine.GlobalInstance) -> int:
        self.global_vars.append(global_)
        return len(self.global_vars) - 1

    def set_global(self, globaladdr: int, value: values.Value) -> None:
        self.global_vars[globaladdr].value = value

    def get_global(self, globaladdr: int) -> machine.GlobalInstance:
        return self.global_vars[globaladdr]

    def add_data(self, data: bytes) -> int:
        self.datas.append(data)
        return len(self.datas) - 1

    def get_data(self, dataaddr: int) -> bytes | None:
        return self.datas[dataaddr]

    def drop_data(self, dataaddr: int) -> None:
        self.datas[dataaddr] = None

    def add_element(self, element: machine.ElementSegmentInstance) -> int:
        self.element_segments.append(element)
        return len(self.element_segments) - 1

    def get_element(self, elementaddr: int) -> machine.ElementSegmentInstance | None:
        return self.element_segments[elementaddr]

    def drop_element(self, elementaddr: int) -> None:
        self.element_segments[elementaddr] = None

    def get_nth_value_of_type(self, n: int, value_type: Type[T]) -> T:
        return self.stack_.get_nth_value_of_type(n, value_type)

    def _debug_stack(self) -> None:
        print("Top of stack:")
        for v in reversed(self.stack_.data):
            print(f"  {v}")
        print("Bottom of stack")
