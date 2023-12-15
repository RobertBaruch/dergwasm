using System;
using System.Collections.Generic;
using System.Linq;

namespace Derg
{
    public static class InstructionEvaluation
    {
        // Executes a single instruction. After the instruction is executed, the current
        // frame's program counter will be incremented. Therefore, instructions that don't
        // do that (e.g. branches) must set the program counter to the desired program counter
        // minus one.
        public static void Execute(Instruction instruction, Machine machine, Frame frame)
        {
            if (!Map.TryGetValue(instruction.Type, out var implementation))
                throw new ArgumentException($"Unimplemented instruction: {instruction.Type}");

            if (machine.Debug)
            {
                ModuleFunc func = frame.Func;
                string operands = string.Join(
                    ", ",
                    (from op in instruction.Operands select $"{op}")
                );
                Console.WriteLine(
                    $"{func.ModuleName}.{func.Name} [{frame.PC}] {instruction.Type} {operands}"
                );
            }

            implementation(instruction, machine, frame);
            frame = machine.Frame;

            if (machine.Debug)
            {
                if (frame.StackLevel() > 0)
                {
                    Console.WriteLine(
                        $"   Top of stack <{frame.StackLevel() - 1}>: {frame.TopOfStack}"
                    );
                }
                else
                {
                    Console.WriteLine($"   Top of stack: <empty>");
                }
            }
            frame.PC++;

            // If we ran off the end of the function, we return from the function.

            if (frame.PC >= frame.Code.Count)
            {
                machine.PopFrame();
                frame = machine.Frame;
                frame.PC++;
            }
        }

        public static IReadOnlyDictionary<
            InstructionType,
            Action<Instruction, Machine, Frame>
        > Map = new Dictionary<InstructionType, Action<Instruction, Machine, Frame>>()
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
            { InstructionType.F32_ABS, NumericInstructions.F32Abs },
            { InstructionType.F32_ADD, NumericInstructions.F32Add },
            { InstructionType.F32_CEIL, NumericInstructions.F32Ceil },
            { InstructionType.F32_CONST, NumericInstructions.Const },
            { InstructionType.F32_CONVERT_I32_S, NumericInstructions.F32ConvertI32S },
            { InstructionType.F32_CONVERT_I32_U, NumericInstructions.F32ConvertI32U },
            { InstructionType.F32_CONVERT_I64_S, NumericInstructions.F32ConvertI64S },
            { InstructionType.F32_CONVERT_I64_U, NumericInstructions.F32ConvertI64U },
            { InstructionType.F32_COPYSIGN, NumericInstructions.F32Copysign },
            { InstructionType.F32_DEMOTE_F64, NumericInstructions.F32DemoteF64 },
            { InstructionType.F32_DIV, NumericInstructions.F32Div },
            { InstructionType.F32_EQ, NumericInstructions.F32Eq },
            { InstructionType.F32_FLOOR, NumericInstructions.F32Floor },
            { InstructionType.F32_GE, NumericInstructions.F32Ge },
            { InstructionType.F32_GT, NumericInstructions.F32Gt },
            { InstructionType.F32_LE, NumericInstructions.F32Le },
            { InstructionType.F32_LOAD, MemoryInstructions.F32Load },
            { InstructionType.F32_LT, NumericInstructions.F32Lt },
            { InstructionType.F32_MAX, NumericInstructions.F32Max },
            { InstructionType.F32_MIN, NumericInstructions.F32Min },
            { InstructionType.F32_MUL, NumericInstructions.F32Mul },
            { InstructionType.F32_NE, NumericInstructions.F32Ne },
            { InstructionType.F32_NEAREST, NumericInstructions.F32Nearest },
            { InstructionType.F32_NEG, NumericInstructions.F32Neg },
            { InstructionType.F32_REINTERPRET_I32, NumericInstructions.F32ReinterpretI32 },
            { InstructionType.F32_SQRT, NumericInstructions.F32Sqrt },
            { InstructionType.F32_STORE, MemoryInstructions.F32Store },
            { InstructionType.F32_SUB, NumericInstructions.F32Sub },
            { InstructionType.F32_TRUNC, NumericInstructions.F32Trunc },
            { InstructionType.F64_ABS, NumericInstructions.F64Abs },
            { InstructionType.F64_ADD, NumericInstructions.F64Add },
            { InstructionType.F64_CEIL, NumericInstructions.F64Ceil },
            { InstructionType.F64_CONST, NumericInstructions.Const },
            { InstructionType.F64_CONVERT_I32_S, NumericInstructions.F64ConvertI32S },
            { InstructionType.F64_CONVERT_I32_U, NumericInstructions.F64ConvertI32U },
            { InstructionType.F64_CONVERT_I64_S, NumericInstructions.F64ConvertI64S },
            { InstructionType.F64_CONVERT_I64_U, NumericInstructions.F64ConvertI64U },
            { InstructionType.F64_COPYSIGN, NumericInstructions.F64Copysign },
            { InstructionType.F64_DIV, NumericInstructions.F64Div },
            { InstructionType.F64_EQ, NumericInstructions.F64Eq },
            { InstructionType.F64_FLOOR, NumericInstructions.F64Floor },
            { InstructionType.F64_GE, NumericInstructions.F64Ge },
            { InstructionType.F64_GT, NumericInstructions.F64Gt },
            { InstructionType.F64_LE, NumericInstructions.F64Le },
            { InstructionType.F64_LOAD, MemoryInstructions.F64Load },
            { InstructionType.F64_LT, NumericInstructions.F64Lt },
            { InstructionType.F64_MAX, NumericInstructions.F64Max },
            { InstructionType.F64_MIN, NumericInstructions.F64Min },
            { InstructionType.F64_MUL, NumericInstructions.F64Mul },
            { InstructionType.F64_NE, NumericInstructions.F64Ne },
            { InstructionType.F64_NEAREST, NumericInstructions.F64Nearest },
            { InstructionType.F64_NEG, NumericInstructions.F64Neg },
            { InstructionType.F64_PROMOTE_F32, NumericInstructions.F64PromoteF32 },
            { InstructionType.F64_REINTERPRET_I64, NumericInstructions.F64ReinterpretI64 },
            { InstructionType.F64_SQRT, NumericInstructions.F64Sqrt },
            { InstructionType.F64_STORE, MemoryInstructions.F64Store },
            { InstructionType.F64_SUB, NumericInstructions.F64Sub },
            { InstructionType.F64_TRUNC, NumericInstructions.F64Trunc },
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
            { InstructionType.I32_REINTERPRET_F32, NumericInstructions.I32ReinterpretF32 },
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
            { InstructionType.I32_TRUNC_F32_S, NumericInstructions.I32TruncF32S },
            { InstructionType.I32_TRUNC_F32_U, NumericInstructions.I32TruncF32U },
            { InstructionType.I32_TRUNC_F64_S, NumericInstructions.I32TruncF64S },
            { InstructionType.I32_TRUNC_F64_U, NumericInstructions.I32TruncF64U },
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
            { InstructionType.I64_REINTERPRET_F64, NumericInstructions.I64ReinterpretF64 },
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
            { InstructionType.I64_TRUNC_F32_S, NumericInstructions.I64TruncF32S },
            { InstructionType.I64_TRUNC_F32_U, NumericInstructions.I64TruncF32U },
            { InstructionType.I64_TRUNC_F64_S, NumericInstructions.I64TruncF64S },
            { InstructionType.I64_TRUNC_F64_U, NumericInstructions.I64TruncF64U },
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
