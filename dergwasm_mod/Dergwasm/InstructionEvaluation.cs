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
                { InstructionType.BR, ControlInstructions.Br },
                { InstructionType.BR_IF, ControlInstructions.BrIf },
                { InstructionType.BR_TABLE, ControlInstructions.BrTable },
                { InstructionType.BLOCK, ControlInstructions.Block },
                { InstructionType.CALL, ControlInstructions.Call },
                { InstructionType.CALL_INDIRECT, ControlInstructions.CallIndirect },
                { InstructionType.DATA_DROP, MemoryInstructions.DataDrop },
                { InstructionType.DROP, ParametricInstructions.Drop },
                { InstructionType.ELEM_DROP, TableInstructions.ElemDrop },
                { InstructionType.ELSE, ControlInstructions.Else },
                { InstructionType.END, ControlInstructions.End },
                { InstructionType.F32_CONST, NumericInstructions.Const },
                { InstructionType.F32_LOAD, MemoryInstructions.F32Load },
                { InstructionType.F64_CONST, NumericInstructions.Const },
                { InstructionType.F64_LOAD, MemoryInstructions.F64Load },
                { InstructionType.GLOBAL_GET, VariableInstructions.GlobalGet },
                { InstructionType.GLOBAL_SET, VariableInstructions.GlobalSet },
                { InstructionType.I32_CLZ, NumericInstructions.I32Clz },
                { InstructionType.I32_CONST, NumericInstructions.Const },
                { InstructionType.I32_CTZ, NumericInstructions.I32Ctz },
                { InstructionType.I32_LOAD, MemoryInstructions.I32Load },
                { InstructionType.I32_LOAD8_S, MemoryInstructions.I32Load8_S },
                { InstructionType.I32_LOAD8_U, MemoryInstructions.I32Load8_U },
                { InstructionType.I32_LOAD16_S, MemoryInstructions.I32Load16_S },
                { InstructionType.I32_LOAD16_U, MemoryInstructions.I32Load16_U },
                { InstructionType.I32_STORE, MemoryInstructions.I32Store },
                { InstructionType.I32_STORE8, MemoryInstructions.I32Store8 },
                { InstructionType.I32_STORE16, MemoryInstructions.I32Store16 },
                { InstructionType.I64_CLZ, NumericInstructions.I64Clz },
                { InstructionType.I64_CONST, NumericInstructions.Const },
                { InstructionType.I64_CTZ, NumericInstructions.I64Ctz },
                { InstructionType.I64_LOAD, MemoryInstructions.I64Load },
                { InstructionType.I64_LOAD8_S, MemoryInstructions.I64Load8_S },
                { InstructionType.I64_LOAD8_U, MemoryInstructions.I64Load8_U },
                { InstructionType.I64_LOAD16_S, MemoryInstructions.I64Load16_S },
                { InstructionType.I64_LOAD16_U, MemoryInstructions.I64Load16_U },
                { InstructionType.I64_LOAD32_S, MemoryInstructions.I64Load32_S },
                { InstructionType.I64_LOAD32_U, MemoryInstructions.I64Load32_U },
                { InstructionType.I64_STORE, MemoryInstructions.I64Store },
                { InstructionType.I64_STORE8, MemoryInstructions.I64Store8 },
                { InstructionType.I64_STORE16, MemoryInstructions.I64Store16 },
                { InstructionType.I64_STORE32, MemoryInstructions.I64Store32 },
                { InstructionType.IF, ControlInstructions.If },
                { InstructionType.LOCAL_GET, VariableInstructions.LocalGet },
                { InstructionType.LOCAL_SET, VariableInstructions.LocalSet },
                { InstructionType.LOCAL_TEE, VariableInstructions.LocalTee },
                { InstructionType.LOOP, ControlInstructions.Loop },
                { InstructionType.MEMORY_COPY, MemoryInstructions.MemoryCopy },
                { InstructionType.MEMORY_FILL, MemoryInstructions.MemoryFill },
                { InstructionType.MEMORY_GROW, MemoryInstructions.MemoryGrow },
                { InstructionType.MEMORY_INIT, MemoryInstructions.MemoryInit },
                { InstructionType.MEMORY_SIZE, MemoryInstructions.MemorySize },
                { InstructionType.NOP, ControlInstructions.Nop },
                { InstructionType.REF_IS_NULL, ReferenceInstructions.RefIsNull },
                { InstructionType.REF_FUNC, ReferenceInstructions.Ref },
                { InstructionType.REF_NULL, ReferenceInstructions.RefNull },
                { InstructionType.RETURN, ControlInstructions.Return },
                { InstructionType.SELECT, ParametricInstructions.Select },
                { InstructionType.TABLE_COPY, TableInstructions.TableCopy },
                { InstructionType.TABLE_GET, TableInstructions.TableGet },
                { InstructionType.TABLE_GROW, TableInstructions.TableGrow },
                { InstructionType.TABLE_INIT, TableInstructions.TableInit },
                { InstructionType.TABLE_FILL, TableInstructions.TableFill },
                { InstructionType.TABLE_SET, TableInstructions.TableSet },
                { InstructionType.TABLE_SIZE, TableInstructions.TableSize },
                { InstructionType.UNREACHABLE, ControlInstructions.Unreachable },
            };
    }
}
