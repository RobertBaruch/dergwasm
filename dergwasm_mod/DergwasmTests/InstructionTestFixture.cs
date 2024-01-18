using System.Collections.Generic;
using System.Linq;
using Derg;

namespace DergwasmTests
{
    public class InstructionTestFixture
    {
        public TestMachine machine = new TestMachine();
        public List<Instruction> program;

        public InstructionTestFixture() { }

        public UnflattenedInstruction Insn(InstructionType type, params Value[] operands)
        {
            return new UnflattenedInstruction(
                type,
                (from operand in operands select new UnflattenedOperand(operand)).ToArray()
            );
        }

        public UnflattenedInstruction Nop() => Insn(InstructionType.NOP);

        public UnflattenedInstruction End() => Insn(InstructionType.END);

        public UnflattenedInstruction Else() => Insn(InstructionType.ELSE);

        public UnflattenedInstruction Drop() => Insn(InstructionType.DROP);

        public UnflattenedInstruction Return() => Insn(InstructionType.RETURN);

        public UnflattenedInstruction Call(int v) =>
            Insn(InstructionType.CALL, new Value { s32 = v });

        public UnflattenedInstruction CallIndirect(int typeidx, int tableidx)
        {
            return Insn(
                InstructionType.CALL_INDIRECT,
                new Value { s32 = typeidx },
                new Value { s32 = tableidx }
            );
        }

        public UnflattenedInstruction I32Const(int v) =>
            Insn(InstructionType.I32_CONST, new Value { s32 = v });

        public UnflattenedInstruction I32Const(uint v) =>
            Insn(InstructionType.I32_CONST, new Value { u32 = v });

        public UnflattenedInstruction I64Const(long v) =>
            Insn(InstructionType.I64_CONST, new Value { s64 = v });

        public UnflattenedInstruction I64Const(ulong v) =>
            Insn(InstructionType.I64_CONST, new Value { u64 = v });

        public UnflattenedInstruction F32Const(float v) =>
            Insn(InstructionType.F32_CONST, new Value { f32 = v });

        public UnflattenedInstruction F64Const(double v) =>
            Insn(InstructionType.F64_CONST, new Value { f64 = v });

        public UnflattenedInstruction Br(int levels) =>
            Insn(InstructionType.BR, new Value { s32 = levels });

        // A block with zero args and zero returns.
        public UnflattenedInstruction VoidBlock(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.BLOCK,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value { u64 = 0UL, value_hi = (ulong)BlockType.VOID_BLOCK },
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>()
                    ),
                }
            );
        }

        // A block with zero args and an I32 return.
        public UnflattenedInstruction I32Block(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.BLOCK,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value
                        {
                            u64 = 0UL,
                            value_hi = (ulong)BlockType.RETURNING_BLOCK | (ulong)ValueType.I32 << 2
                        },
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>()
                    ),
                }
            );
        }

        // A block with an I32 arg and no returns.
        public UnflattenedInstruction I32_VoidBlock(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.BLOCK,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value
                        {
                            u64 = 0UL,
                            value_hi = (ulong)BlockType.TYPED_BLOCK | (1UL << 2)
                        },
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>()
                    ),
                }
            );
        }

        // An IF with no ELSE, zero args and zero returns.
        public UnflattenedInstruction VoidIf(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.IF,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value { u64 = 0UL, value_hi = (ulong)BlockType.VOID_BLOCK },
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>()
                    ),
                }
            );
        }

        // An IF with an ELSE, zero args and zero returns.
        public UnflattenedInstruction VoidIfElse(
            UnflattenedInstruction[] instructions,
            UnflattenedInstruction[] else_instructions
        )
        {
            return new UnflattenedInstruction(
                InstructionType.IF,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value { u64 = 0UL, value_hi = (ulong)BlockType.VOID_BLOCK },
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>(else_instructions)
                    ),
                }
            );
        }

        // A loop with zero args and zero returns.
        public UnflattenedInstruction VoidLoop(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.LOOP,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value { u64 = 0UL, value_hi = (ulong)BlockType.VOID_BLOCK },
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>()
                    ),
                }
            );
        }

        // A loop with an I32 arg and no return.
        public UnflattenedInstruction I32Loop(params UnflattenedInstruction[] instructions)
        {
            return new UnflattenedInstruction(
                InstructionType.LOOP,
                new UnflattenedOperand[]
                {
                    new UnflattenedBlockOperand(
                        new Value
                        {
                            u64 = 0UL,
                            value_hi = (ulong)BlockType.TYPED_BLOCK | (1UL << 2)
                        },
                        new List<UnflattenedInstruction>(instructions),
                        new List<UnflattenedInstruction>()
                    ),
                }
            );
        }
    }
}
