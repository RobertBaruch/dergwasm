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
    mems: list[bytearray]
    global_vars: list[machine.GlobalInstance]
    datas: list[bytes]

    def __init__(self) -> None:
        self.current_frame = None
        self.stack_ = stack.Stack()
        self.funcs = []
        self.tables = []
        self.mems = []
        self.global_vars = []
        self.datas = []

    def push(self, value: values.StackValue) -> None:
        self.stack_.push(value)

    def pop(self) -> values.StackValue:
        return self.stack_.pop()

    def peek(self) -> values.StackValue:
        return self.stack_.data[-1]

    # TODO: How does this now interact with global initializer exprs?
    def execute_seq(self, expr: list[Instruction]) -> None:
        """Execute the instructions until RETURN or falling off end."""
        self.get_current_frame().pc = 0
        while self.get_current_frame().pc < len(expr):
            instruction = expr[self.get_current_frame().pc]

            if instruction.instruction_type == InstructionType.RETURN:
                insn_eval.eval_insn(self, instruction)
                return

            else:
                insn_eval.eval_insn(self, instruction)

        f = self.get_current_frame()
        n = f.arity
        results = [self.pop() for _ in range(n)]
        # There might be a label here, depending on whether we fell off the end.
        # A BR will have slid the label out, but falling off the end will not have.
        frame = self.pop()
        if isinstance(frame, values.Label):
            frame = self.pop()
            assert isinstance(frame, values.Frame)
        # Push the results back on the stack
        for v in reversed(results):
            self.push(v)
        self.current_frame = frame.prev_frame

    def execute_expr(self, expr: list[Instruction]) -> list[Instruction]:
        raise NotImplementedError()

    def get_current_frame(self) -> values.Frame:
        if self.current_frame is None:
            self.current_frame = self.stack_.get_topmost_value_of_type(values.Frame)
        return self.current_frame

    def new_frame(self, frame: values.Frame) -> None:
        frame.prev_frame = self.current_frame
        self.current_frame = frame
        self.push(frame)

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

        # We didn't execute a RETURN instruction (or fall off the end), so we returned
        # via a BR instruction. That means we slid out the label, but not the frame.
        # So we do that here.
        # f = self.get_current_frame()
        # n = f.arity
        # results = [self.pop() for _ in range(n)]
        # frame = self.pop()
        # assert isinstance(frame, values.Frame)
        # # Push the results back on the stack
        # for v in reversed(results):
        #     self.push(v)

    def add_table(self, table: machine.TableInstance) -> int:
        self.tables.append(table)
        return len(self.tables) - 1

    def get_table(self, tableidx: int) -> machine.TableInstance:
        return self.tables[tableidx]

    def add_mem(self, mem: bytearray) -> int:
        self.mems.append(mem)
        return len(self.mems) - 1

    def get_mem(self, memidx: int) -> bytearray:
        return self.mems[memidx]

    def add_global(self, global_: machine.GlobalInstance) -> int:
        self.global_vars.append(global_)
        return len(self.global_vars) - 1

    def set_global(self, globalidx: int, value: values.Value) -> None:
        self.global_vars[globalidx].value = value

    def get_global(self, globalidx: int) -> machine.GlobalInstance:
        return self.global_vars[globalidx]

    def add_data(self, data: bytes) -> int:
        self.datas.append(data)
        return len(self.datas) - 1

    def get_data(self, dataidx: int) -> bytes:
        return self.datas[dataidx]

    def get_nth_value_of_type(
        self, n: int, value_type: Type[values.StackValue]
    ) -> values.StackValue:
        return self.stack_.get_nth_value_of_type(n, value_type)
