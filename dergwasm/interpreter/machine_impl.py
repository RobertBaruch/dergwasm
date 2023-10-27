"""Concrete implementation of the machine interface."""

from typing import Type

from dergwasm.interpreter.binary import FuncType
from dergwasm.interpreter import stack
from dergwasm.interpreter import machine
from dergwasm.interpreter import values
from dergwasm.interpreter.insn import Instruction, InstructionType, Block
from dergwasm.interpreter import insn_eval


class MachineImpl(machine.Machine):
    """The state of the machine."""

    stack_: stack.Stack
    funcs: list[machine.FuncInstance]
    tables: list[machine.TableInstance]
    mems: list[bytearray]
    global_vars: list[machine.GlobalInstance]
    datas: list[bytes]

    def __init__(self) -> None:
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

    def _block(self, block: Block, continuation_pc: int) -> None:
        f = self.get_current_frame()
        block_func_type = block.block_type
        if isinstance(block_func_type, int):
            block_func_type = f.module.func_types[block_func_type]
        elif block_func_type is not None:
            block_func_type = FuncType([], [block_func_type])
        else:
            block_func_type = FuncType([], [])

        # Create a label for the block. Its continuation is the end of the
        # block.
        label = values.Label(len(block_func_type.results), continuation_pc)
        # Slide the label under the params.
        block_vals = [self.pop() for _ in block_func_type.parameters]
        self.push(label)
        for v in reversed(block_vals):
            self.push(v)

    def _loop(self, block: Block, continuation_pc: int) -> int:
        f = self.get_current_frame()
        block_func_type = block.block_type
        if isinstance(block_func_type, int):
            block_func_type = f.module.func_types[block_func_type]
        elif block_func_type is not None:
            block_func_type = FuncType([], [block_func_type])
        else:
            block_func_type = FuncType([], [])

        label = values.Label(len(block_func_type.parameters), continuation_pc)
        # Slide the label under the params.
        block_vals = [self.pop() for _ in block_func_type.parameters]
        self.push(label)
        for v in reversed(block_vals):
            self.push(v)

    def _else(self) -> int:
        # End of block reached without jump. Slide the first label out of the
        # stack, and then jump to after its end.
        stack_values = []
        value = self.pop()
        while not isinstance(value, values.Label):
            stack_values.append(value)
            value = self.pop()

        label = value
        print(f"END, label encountered: {label}")
        # Push the vals back on the stack
        while stack_values:
            self.push(stack_values.pop())
        return label.continuation

    def _end(self) -> None:
        # End of block reached without jump. Slide the first label out of the
        # stack, and then jump to after its end.
        stack_values = []
        value = self.pop()
        while not isinstance(value, values.Label):
            stack_values.append(value)
            value = self.pop()

        label = value
        print(f"END, label encountered: {label}")
        # Push the vals back on the stack
        while stack_values:
            self.push(stack_values.pop())

    def _return(self) -> None:
        f = self.get_current_frame()
        n = f.arity
        results = [self.pop() for _ in range(n)]
        # Pop everything up to and including the frame. This will also include
        # the function's label. Basically skip all nesting levels.
        frame = self.pop()
        while not isinstance(frame, values.Frame):
            frame = self.pop()
        # Push the results back on the stack
        for v in reversed(results):
            self.push(v)

    def _br(self, level: int) -> int:
        # Branch out of a nested block. The sole exception is BR 0 when not
        # in a block. That is the equivalent of a return.
        # Find the level-th label (0-based) on the stack.
        label: values.Label = self.get_nth_value_of_type(level, values.Label)
        n = label.arity

        # save the top n values on the stack
        vals = [self.pop() for _ in range(n)]
        # pop everything up to the label
        while level >= 0:
            value = self.pop()
            if isinstance(value, values.Label):
                level -= 1
        # push the saved values back on the stack
        for v in reversed(vals):
            self.push(v)
        return label.continuation

    # TODO: How does this now interact with global initializer exprs?
    def execute_seq(self, expr: list[Instruction]) -> None:
        # Execute the instructions one by one until we hit a block, loop, if, br,
        # br_if, br_table, or return instruction.
        pc = 0
        while pc < len(expr):
            instruction = expr[pc]
            operands = instruction.operands

            if instruction.instruction_type == InstructionType.BLOCK:
                self._block(operands[0], instruction.continuation_pc)
                pc += 1

            elif instruction.instruction_type == InstructionType.END:
                self._end()
                pc += 1

            elif instruction.instruction_type == InstructionType.ELSE:
                pc = self._else()

            elif instruction.instruction_type == InstructionType.RETURN:
                self._return()
                return

            elif instruction.instruction_type == InstructionType.BR:
                assert isinstance(operands[0], int)
                pc = self._br(operands[0])

            elif instruction.instruction_type == InstructionType.BR_IF:
                assert isinstance(operands[0], int)
                cond: values.Value = self.pop()
                if cond.value:
                    pc = self._br(operands[0])
                else:
                    pc += 1

            elif instruction.instruction_type == InstructionType.BR_TABLE:
                idx_value: values.Value = self.pop()
                assert isinstance(idx_value.value, int)
                idx = idx_value.value
                if idx < len(operands):
                    pc = self._br(operands[idx])
                else:
                    pc = self._br(operands[-1])

            elif instruction.instruction_type == InstructionType.IF:
                assert isinstance(operands[0], Block)
                cond: values.Value = self.pop()
                if cond.value:
                    self._block(operands[0], instruction.continuation_pc)
                    pc += 1
                else:
                    # If there's no else clause, don't start a block.
                    if instruction.else_continuation_pc != instruction.continuation_pc:
                        self._block(operands[0], instruction.continuation_pc)
                    pc = instruction.else_continuation_pc

            elif instruction.instruction_type == InstructionType.LOOP:
                assert isinstance(operands[0], Block)
                self._loop(operands[0], instruction.continuation_pc)
                pc += 1

            else:
                insn_eval.eval_insn(self, instruction)
                pc = instruction.continuation_pc

        # Since all nesting blocks end in an END, this can only happen if we fell
        # off the end of a function. It's the equivalent of a return.
        f = self.get_current_frame()
        n = f.arity
        results = [self.pop() for _ in range(n)]
        # There might be a label here, depending on whether we fell off the end.
        # A BR will have slid the label out, but falling off the end will not have.
        label_or_frame = self.pop()
        if isinstance(label_or_frame, values.Label):
            frame = self.pop()
            assert isinstance(frame, values.Frame)
        # Push the results back on the stack
        for v in reversed(results):
            self.push(v)

    def execute_expr(self, expr: list[Instruction]) -> list[Instruction]:
        raise NotImplementedError()

    def get_current_frame(self) -> values.Frame:
        return self.stack_.get_topmost_value_of_type(values.Frame)

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
        self.push(values.Frame(len(func_type.results), local_vars, f.module))
        # The continuation is a special END instruction which we detect in order to
        # determine whether we fell off the end of the function (END) or returned
        # (no END).
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
