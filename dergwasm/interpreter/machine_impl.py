"""Concrete implementation of the machine interface."""

from typing import Type

from binary import FuncType
import stack
import machine
import values
from insn import Instruction, InstructionType, Block
import insn_eval


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

    def _block(self, block: Block, use_else: bool = False) -> int:
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
        label = values.Label(len(block_func_type.results), 0)
        # Slide the label under the params.
        block_vals = [self.pop() for _ in block_func_type.parameters]
        self.push(label)
        for v in reversed(block_vals):
            self.push(v)

        # Execute the block's instruction sequence.
        return self.execute_seq(
            block.else_instructions if use_else else block.instructions
        )

    def _loop(self, block: Block) -> int:
        f = self.get_current_frame()
        block_func_type = block.block_type
        if isinstance(block_func_type, int):
            block_func_type = f.module.func_types[block_func_type]
        elif block_func_type is not None:
            block_func_type = FuncType([], [block_func_type])
        else:
            block_func_type = FuncType([], [])

        while True:
            # Create a label for the block. Its continuation is the *loop itself*.
            # So a BR 0 in the block will result in another loop, rather than ending it.
            # And, running off the end of the loop is a normal end.
            #
            # -2 is a special continuation value which tells us to quit the loop.
            label = values.Label(len(block_func_type.parameters), -2)
            # Slide the label under the params.
            block_vals = [self.pop() for _ in block_func_type.parameters]
            self.push(label)
            for v in reversed(block_vals):
                self.push(v)

            skip_levels = self.execute_seq(block.instructions)
            # Return out of loop?
            if skip_levels == -1:
                return -1
            # Exit loop?
            if skip_levels == -2:
                return 0
            # Skip nesting levels?
            if skip_levels != 0:
                return skip_levels - 1

    def _end(self) -> int:
        # End of block reached without jump. Slide the first label out of the
        # stack, and then jump to after its end. Unless the label was for a loop,
        # in which case jump to the beginning of the loop.
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
        # 0 = Jump to after the end: don't skip nesting levels.
        # -2 = Jump to the beginning of the loop.
        return label.continuation

    def _return(self) -> int:
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
        # Skip all the way out to the original invoke_func.
        return -1

    def _br(self, level: int) -> int:
        # Branch out of a nested block. The sole exception is BR 0 when not
        # in a block. That is the equivalent of a return.
        original_level = level
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
        return original_level

    # TODO: How does this now interact with global initializer exprs?
    def execute_seq(self, expr: list[Instruction]) -> int:
        # Execute the instructions one by one until we hit a block, loop, if, br,
        # br_if, br_table, or return instruction.
        i = 0
        while i < len(expr):
            instruction = expr[i]
            operands = instruction.operands

            if instruction.instruction_type == InstructionType.BLOCK:
                skip_levels = self._block(operands[0])
                if skip_levels == -1:
                    return -1
                if skip_levels > 0:
                    return skip_levels - 1
                i += 1

            elif instruction.instruction_type == InstructionType.END:
                return self._end()

            elif instruction.instruction_type == InstructionType.RETURN:
                return self._return()

            elif instruction.instruction_type == InstructionType.BR:
                assert isinstance(operands[0], int)
                return self._br(operands[0])

            elif instruction.instruction_type == InstructionType.BR_IF:
                assert isinstance(operands[0], int)
                cond: values.Value = self.pop()
                if cond.value:
                    return self._br(operands[0])
                i += 1

            elif instruction.instruction_type == InstructionType.BR_TABLE:
                idx_value: values.Value = self.pop()
                assert isinstance(idx_value.value, int)
                idx = idx_value.value
                if idx < len(operands):
                    return self._br(operands[idx])
                return self._br(operands[-1])

            elif instruction.instruction_type == InstructionType.IF:
                assert isinstance(operands[0], Block)
                cond: values.Value = self.pop()
                if cond.value:
                    skip_levels = self._block(operands[0], use_else=False)
                else:
                    skip_levels = self._block(operands[0], use_else=True)
                if skip_levels == -1:
                    return -1
                if skip_levels > 0:
                    return skip_levels - 1
                i += 1

            elif instruction.instruction_type == InstructionType.LOOP:
                assert isinstance(operands[0], Block)
                skip_levels = self._loop(operands[0])
                if skip_levels == -1:
                    return -1
                if skip_levels > 0:
                    return skip_levels - 1
                i += 1

            else:
                insn_eval.eval_insn(instruction.instruction_type, operands, self)
                i += 1

        # Since all nesting blocks end in an END, this can only happen if we fell
        # off the end of a function. It's the equivalent of a return.
        f = self.get_current_frame()
        n = f.arity
        results = [self.pop() for _ in range(n)]
        # There's a label here, too.
        label = self.pop()
        assert isinstance(label, values.Label)
        frame = self.pop()
        assert isinstance(frame, values.Frame)
        # Push the results back on the stack
        for v in reversed(results):
            self.push(v)
        return -1

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
        self.push(values.Label(len(func_type.results), 0))

        skip_levels = self.execute_seq(f.body)
        if skip_levels == -1:
            return
        # We didn't execute a RETURN instruction (or fall off the end), so we returned
        # via a BR instruction. That means we slid out the label, but not the frame.
        # So we do that here.
        f = self.get_current_frame()
        n = f.arity
        results = [self.pop() for _ in range(n)]
        frame = self.pop()
        assert isinstance(frame, values.Frame)
        # Push the results back on the stack
        for v in reversed(results):
            self.push(v)

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
