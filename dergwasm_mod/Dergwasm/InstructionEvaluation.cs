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
                { InstructionType.I32_ADD, NumericInstructions.I32Add },
                { InstructionType.I32_AND, NumericInstructions.I32And },
                { InstructionType.I32_CLZ, NumericInstructions.I32Clz },
                { InstructionType.I32_CONST, NumericInstructions.Const },
                { InstructionType.I32_CTZ, NumericInstructions.I32Ctz },
                { InstructionType.I32_DIV_S, NumericInstructions.I32DivS },
                { InstructionType.I32_DIV_U, NumericInstructions.I32DivU },
                { InstructionType.I32_EQ, NumericInstructions.I32Eq },
                { InstructionType.I32_EQZ, NumericInstructions.I32Eqz },
                { InstructionType.I32_EXTEND8_S, NumericInstructions.I32Extend8S },
                { InstructionType.I32_EXTEND16_S, NumericInstructions.I32Extend16S },
                { InstructionType.I32_GE_S, NumericInstructions.I32GeS },
                { InstructionType.I32_GE_U, NumericInstructions.I32GeU },
                { InstructionType.I32_GT_S, NumericInstructions.I32GtS },
                { InstructionType.I32_GT_U, NumericInstructions.I32GtU },
                { InstructionType.I32_LOAD, MemoryInstructions.I32Load },
                { InstructionType.I32_LOAD8_S, MemoryInstructions.I32Load8_S },
                { InstructionType.I32_LOAD8_U, MemoryInstructions.I32Load8_U },
                { InstructionType.I32_LOAD16_S, MemoryInstructions.I32Load16_S },
                { InstructionType.I32_LOAD16_U, MemoryInstructions.I32Load16_U },
                { InstructionType.I32_LE_S, NumericInstructions.I32LeS },
                { InstructionType.I32_LE_U, NumericInstructions.I32LeU },
                { InstructionType.I32_LT_S, NumericInstructions.I32LtS },
                { InstructionType.I32_LT_U, NumericInstructions.I32LtU },
                { InstructionType.I32_MUL, NumericInstructions.I32Mul },
                { InstructionType.I32_NE, NumericInstructions.I32Ne },
                { InstructionType.I32_OR, NumericInstructions.I32Or },
                { InstructionType.I32_POPCNT, NumericInstructions.I32Popcnt },
                { InstructionType.I32_REM_S, NumericInstructions.I32RemS },
                { InstructionType.I32_REM_U, NumericInstructions.I32RemU },
                { InstructionType.I32_ROTL, NumericInstructions.I32Rotl },
                { InstructionType.I32_ROTR, NumericInstructions.I32Rotr },
                { InstructionType.I32_SHL, NumericInstructions.I32Shl },
                { InstructionType.I32_SHR_S, NumericInstructions.I32ShrS },
                { InstructionType.I32_SHR_U, NumericInstructions.I32ShrU },
                { InstructionType.I32_STORE, MemoryInstructions.I32Store },
                { InstructionType.I32_STORE8, MemoryInstructions.I32Store8 },
                { InstructionType.I32_STORE16, MemoryInstructions.I32Store16 },
                { InstructionType.I32_SUB, NumericInstructions.I32Sub },
                { InstructionType.I32_WRAP_I64, NumericInstructions.I32WrapI64 },
                { InstructionType.I32_XOR, NumericInstructions.I32Xor },
                { InstructionType.I64_ADD, NumericInstructions.I64Add },
                { InstructionType.I64_AND, NumericInstructions.I64And },
                { InstructionType.I64_CLZ, NumericInstructions.I64Clz },
                { InstructionType.I64_CONST, NumericInstructions.Const },
                { InstructionType.I64_CTZ, NumericInstructions.I64Ctz },
                { InstructionType.I64_DIV_S, NumericInstructions.I64DivS },
                { InstructionType.I64_DIV_U, NumericInstructions.I64DivU },
                { InstructionType.I64_EQ, NumericInstructions.I64Eq },
                { InstructionType.I64_EQZ, NumericInstructions.I64Eqz },
                { InstructionType.I64_EXTEND8_S, NumericInstructions.I64Extend8S },
                { InstructionType.I64_EXTEND16_S, NumericInstructions.I64Extend16S },
                { InstructionType.I64_EXTEND32_S, NumericInstructions.I64Extend32S },
                { InstructionType.I64_EXTEND_I32_S, NumericInstructions.I64ExtendI32S },
                { InstructionType.I64_EXTEND_I32_U, NumericInstructions.I64ExtendI32U },
                { InstructionType.I64_GE_S, NumericInstructions.I64GeS },
                { InstructionType.I64_GE_U, NumericInstructions.I64GeU },
                { InstructionType.I64_GT_S, NumericInstructions.I64GtS },
                { InstructionType.I64_GT_U, NumericInstructions.I64GtU },
                { InstructionType.I64_LOAD, MemoryInstructions.I64Load },
                { InstructionType.I64_LOAD8_S, MemoryInstructions.I64Load8_S },
                { InstructionType.I64_LOAD8_U, MemoryInstructions.I64Load8_U },
                { InstructionType.I64_LOAD16_S, MemoryInstructions.I64Load16_S },
                { InstructionType.I64_LOAD16_U, MemoryInstructions.I64Load16_U },
                { InstructionType.I64_LOAD32_S, MemoryInstructions.I64Load32_S },
                { InstructionType.I64_LOAD32_U, MemoryInstructions.I64Load32_U },
                { InstructionType.I64_LE_S, NumericInstructions.I64LeS },
                { InstructionType.I64_LE_U, NumericInstructions.I64LeU },
                { InstructionType.I64_LT_S, NumericInstructions.I64LtS },
                { InstructionType.I64_LT_U, NumericInstructions.I64LtU },
                { InstructionType.I64_MUL, NumericInstructions.I64Mul },
                { InstructionType.I64_NE, NumericInstructions.I64Ne },
                { InstructionType.I64_OR, NumericInstructions.I64Or },
                { InstructionType.I64_POPCNT, NumericInstructions.I64Popcnt },
                { InstructionType.I64_REM_S, NumericInstructions.I64RemS },
                { InstructionType.I64_REM_U, NumericInstructions.I64RemU },
                { InstructionType.I64_ROTL, NumericInstructions.I64Rotl },
                { InstructionType.I64_ROTR, NumericInstructions.I64Rotr },
                { InstructionType.I64_SHL, NumericInstructions.I64Shl },
                { InstructionType.I64_SHR_S, NumericInstructions.I64ShrS },
                { InstructionType.I64_SHR_U, NumericInstructions.I64ShrU },
                { InstructionType.I64_STORE, MemoryInstructions.I64Store },
                { InstructionType.I64_STORE8, MemoryInstructions.I64Store8 },
                { InstructionType.I64_STORE16, MemoryInstructions.I64Store16 },
                { InstructionType.I64_STORE32, MemoryInstructions.I64Store32 },
                { InstructionType.I64_SUB, NumericInstructions.I64Sub },
                { InstructionType.I64_XOR, NumericInstructions.I64Xor },
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
