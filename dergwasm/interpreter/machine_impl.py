"""Concrete implementation of the machine interface."""

from typing import Type

from dergwasm.interpreter import stack
from dergwasm.interpreter import machine
from dergwasm.interpreter import values
from dergwasm.interpreter.insn import Instruction, InstructionType
from dergwasm.interpreter import insn_eval


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

    def peek(self) -> values.StackValue:
        return self.stack_.data[-1]

    def clear_stack(self) -> None:
        self.stack_.data = []

    def execute_seq(self, seq: list[Instruction]) -> None:
        """Execute the instructions until RETURN or falling off end."""
        self.get_current_frame().pc = 0
        while self.get_current_frame().pc < len(seq):
            instruction = seq[self.get_current_frame().pc]

            print(f"Executing {instruction}")
            insn_eval.eval_insn(self, instruction)
            if instruction.instruction_type == InstructionType.RETURN:
                return

        f = self.get_current_frame()
        n = f.arity
        results = [self.pop() for _ in range(n)]
        # There might be a label here, depending on whether we fell off the end.
        # A BR will have slid the label out, but falling off the end will not have.
        self.pop_to_frame()
        # Push the results back on the stack
        for v in reversed(results):
            self.push(v)

    def execute_expr(self, expr: list[Instruction]) -> list[Instruction]:
        """Execute the instructions until RETURN or falling off end."""
        self.get_current_frame().pc = 0
        while self.get_current_frame().pc < len(expr):
            instruction = expr[self.get_current_frame().pc]

            if instruction.instruction_type == InstructionType.RETURN:
                raise RuntimeError(
                    "Unexpected RETURN instruction in an initialization expression"
                )
            insn_eval.eval_insn(self, instruction)

    def get_current_frame(self) -> values.Frame:
        if self.current_frame is None:
            self.current_frame = self.stack_.get_topmost_value_of_type(values.Frame)
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

    def get_func(self, funcidx: int) -> machine.FuncInstance:
        return self.funcs[funcidx]

    def invoke_func(self, funcidx: int) -> None:
        """Invokes a function, returning when the function ends/returns/traps."""
        f = self.funcs[funcidx]
        assert isinstance(f, machine.ModuleFuncInstance)
        func_type = f.functype
        local_vars: list[values.Value] = [self.pop() for _ in func_type.parameters]
        local_vars.extend([values.StackValue.default(v) for v in f.local_vars])
        self.new_frame(values.Frame(len(func_type.results), local_vars, f.module, 0))
        self.push(values.Label(len(func_type.results), len(f.body)))

        self.execute_seq(f.body)

    def add_table(self, table: machine.TableInstance) -> int:
        self.tables.append(table)
        return len(self.tables) - 1

    def get_table(self, tableidx: int) -> machine.TableInstance:
        return self.tables[tableidx]

    def add_mem(self, mem: machine.MemInstance) -> int:
        self.mems.append(mem)
        return len(self.mems) - 1

    def get_mem(self, memidx: int) -> machine.MemInstance:
        return self.mems[memidx]

    def get_mem_data(self, memidx: int) -> bytearray:
        return self.mems[memidx].data

    def add_global(self, global_: machine.GlobalInstance) -> int:
        self.global_vars.append(global_)
        return len(self.global_vars) - 1

    def set_global(self, globalidx: int, value: values.Value) -> None:
        self.global_vars[globalidx].value = value

    def get_global(self, globalidx: int) -> machine.GlobalInstance:
        return self.global_vars[globalidx].value

    def add_data(self, data: bytes) -> int:
        self.datas.append(data)
        return len(self.datas) - 1

    def get_data(self, dataidx: int) -> bytes:
        return self.datas[dataidx]

    def drop_data(self, dataidx: int) -> None:
        self.datas[dataidx] = None

    def add_element(self, element: machine.ElementSegmentInstance) -> int:
        self.element_segments.append(element)
        return len(self.element_segments) - 1

    def get_element(self, elementidx: int) -> machine.ElementSegmentInstance:
        return self.element_segments[elementidx]

    def drop_element(self, elementidx: int) -> None:
        self.element_segments[elementidx] = None

    def get_nth_value_of_type(
        self, n: int, value_type: Type[values.StackValue]
    ) -> values.StackValue:
        return self.stack_.get_nth_value_of_type(n, value_type)
