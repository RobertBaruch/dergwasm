using System;
using System.Collections.Generic;

namespace Derg
{
    public static class InstructionEvaluation
    {
        // Executes a single instruction. After the instruction is executed, the current
        // frame's program counter will be incremented. Therefore, instructions that don't
        // do that (e.g. branches) must set the program counter to the desired program counter
        // minus one.
        public static void Execute(Instruction instruction, IMachine machine)
        {
            if (!Map.TryGetValue(instruction.Type, out var implementation))
                throw new ArgumentException($"Unimplemented instruction: {instruction.Type}");
            implementation(instruction, machine);
            machine.PC++;

            // If we ran off the end of the function, we return from the function.

            if (machine.PC >= machine.Frame.Code.Count)
            {
                machine.PopFrame();
                machine.PC++;
            }
        }

        private static IReadOnlyDictionary<InstructionType, Action<Instruction, IMachine>> Map =
            new Dictionary<InstructionType, Action<Instruction, IMachine>>()
            {
                // Please maintain these sorted alphanumerically by InstructionType.
                { InstructionType.BR, BlockAndControlInstructions.Br },
                { InstructionType.BR_IF, BlockAndControlInstructions.BrIf },
                { InstructionType.BR_TABLE, BlockAndControlInstructions.BrTable },
                { InstructionType.BLOCK, BlockAndControlInstructions.Block },
                { InstructionType.CALL, BlockAndControlInstructions.Call },
                { InstructionType.CALL_INDIRECT, BlockAndControlInstructions.CallIndirect },
                { InstructionType.DROP, StackInstructions.Drop },
                { InstructionType.ELEM_DROP, TableInstructions.ElemDrop },
                { InstructionType.ELSE, BlockAndControlInstructions.Else },
                { InstructionType.END, BlockAndControlInstructions.End },
                { InstructionType.F32_CONST, StackInstructions.Const },
                { InstructionType.F32_LOAD, MemoryInstructions.F32Load },
                { InstructionType.F64_CONST, StackInstructions.Const },
                { InstructionType.F64_LOAD, MemoryInstructions.F64Load },
                { InstructionType.GLOBAL_GET, VariableInstructions.GlobalGet },
                { InstructionType.GLOBAL_SET, VariableInstructions.GlobalSet },
                { InstructionType.I32_CONST, StackInstructions.Const },
                { InstructionType.I32_LOAD, MemoryInstructions.I32Load },
                { InstructionType.I32_LOAD8_S, MemoryInstructions.I32Load8_S },
                { InstructionType.I32_LOAD8_U, MemoryInstructions.I32Load8_U },
                { InstructionType.I32_LOAD16_S, MemoryInstructions.I32Load16_S },
                { InstructionType.I32_LOAD16_U, MemoryInstructions.I32Load16_U },
                { InstructionType.I64_CONST, StackInstructions.Const },
                { InstructionType.I64_LOAD, MemoryInstructions.I64Load },
                { InstructionType.I64_LOAD8_S, MemoryInstructions.I64Load8_S },
                { InstructionType.I64_LOAD8_U, MemoryInstructions.I64Load8_U },
                { InstructionType.I64_LOAD16_S, MemoryInstructions.I64Load16_S },
                { InstructionType.I64_LOAD16_U, MemoryInstructions.I64Load16_U },
                { InstructionType.I64_LOAD32_S, MemoryInstructions.I64Load32_S },
                { InstructionType.I64_LOAD32_U, MemoryInstructions.I64Load32_U },
                { InstructionType.IF, BlockAndControlInstructions.If },
                { InstructionType.LOCAL_GET, VariableInstructions.LocalGet },
                { InstructionType.LOCAL_SET, VariableInstructions.LocalSet },
                { InstructionType.LOCAL_TEE, VariableInstructions.LocalTee },
                { InstructionType.LOOP, BlockAndControlInstructions.Loop },
                { InstructionType.NOP, StackInstructions.Nop },
                { InstructionType.REF_IS_NULL, StackInstructions.RefIsNull },
                { InstructionType.REF_FUNC, StackInstructions.Ref },
                { InstructionType.REF_NULL, StackInstructions.RefNull },
                { InstructionType.RETURN, BlockAndControlInstructions.Return },
                { InstructionType.SELECT, StackInstructions.Select },
                { InstructionType.TABLE_COPY, TableInstructions.TableCopy },
                { InstructionType.TABLE_GET, TableInstructions.TableGet },
                { InstructionType.TABLE_GROW, TableInstructions.TableGrow },
                { InstructionType.TABLE_INIT, TableInstructions.TableInit },
                { InstructionType.TABLE_FILL, TableInstructions.TableFill },
                { InstructionType.TABLE_SET, TableInstructions.TableSet },
                { InstructionType.TABLE_SIZE, TableInstructions.TableSize },
                { InstructionType.UNREACHABLE, StackInstructions.Unreachable },
            };
    }
}
