using System.Linq;
using Dergwasm.Instructions;
using Dergwasm.Runtime;
using Xunit;

namespace DergwasmTests.instructions
{
    public class TableInstructionTests : InstructionTestFixture
    {
        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(0, 1, 2)]
        [InlineData(1, 0, 3)]
        [InlineData(1, 1, 4)]
        public void TestTableGet(int tableidx, int elemidx, int expected)
        {
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(2), ValueType.FUNCREF))
            );
            machine.SetTableAt(
                31,
                new Table("test", "$table1", new TableType(new Limits(2), ValueType.FUNCREF))
            );
            machine.tables[30].Elements[0] = Value.RefOfFuncAddr(1);
            machine.tables[30].Elements[1] = Value.RefOfFuncAddr(2);
            machine.tables[31].Elements[0] = Value.RefOfFuncAddr(3);
            machine.tables[31].Elements[1] = Value.RefOfFuncAddr(4);

            // 0: I32_CONST elemidx
            // 1: TABLE_GET tableidx
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(elemidx),
                Insn(InstructionType.TABLE_GET, new Value { s32 = tableidx }), // Maps to tableidx+30
                Nop()
            );

            machine.Step(2);

            Assert.Collection(
                machine.Frame.value_stack,
                e => Assert.Equal(Value.RefOfFuncAddr(expected), e)
            );
        }

        [Fact]
        public void TestTableGetTrapsOnOutOfBounds()
        {
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(2), ValueType.FUNCREF))
            );

            // 0: I32_CONST 2
            // 1: TABLE_GET 0
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(2),
                Insn(InstructionType.TABLE_GET, new Value { s32 = 0 }), // Maps to table 30
                Nop()
            );

            Assert.Throws<Trap>(() => machine.Step(2));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        public void TestTableSet(int tableidx, int elemidx)
        {
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(2), ValueType.FUNCREF))
            );
            machine.SetTableAt(
                31,
                new Table("test", "$table1", new TableType(new Limits(2), ValueType.FUNCREF))
            );

            // 0: I32_CONST elemidx
            // 1: REF_FUNC 10
            // 2: TABLE_SET tableidx
            // 3: NOP
            machine.SetProgram(
                0,
                I32Const(elemidx),
                Insn(InstructionType.REF_FUNC, new Value { s32 = 10 }), // Maps to addr 20
                Insn(InstructionType.TABLE_SET, new Value { s32 = tableidx }), // Maps to table tableidx+30
                Nop()
            );

            machine.Step(3);

            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(Value.RefOfFuncAddr(20), machine.tables[30 + tableidx].Elements[elemidx]);
        }

        [Fact]
        public void TestTableSetTrapsOnOutOfBounds()
        {
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(2), ValueType.FUNCREF))
            );

            // 0: I32_CONST 2
            // 1: REF_FUNC 10
            // 2: TABLE_SET 0
            // 3: NOP
            machine.SetProgram(
                0,
                I32Const(2),
                Insn(InstructionType.REF_FUNC, new Value { s32 = 10 }), // Maps to addr 20
                Insn(InstructionType.TABLE_SET, new Value { s32 = 0 }), // Maps to table 30
                Nop()
            );

            Assert.Throws<Trap>(() => machine.Step(3));
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(1, 3)]
        public void TestTableSize(int tableidx, int expected)
        {
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(2), ValueType.FUNCREF))
            );
            machine.SetTableAt(
                31,
                new Table("test", "$table1", new TableType(new Limits(3), ValueType.FUNCREF))
            );

            // 0: TABLE_SIZE tableidx
            // 1: NOP
            machine.SetProgram(
                0,
                Insn(InstructionType.TABLE_SIZE, new Value { s32 = tableidx }), // Maps to tableidx+30
                Nop()
            );

            machine.Step();

            Assert.Equal(expected, machine.Frame.TopOfStack.s32);
        }

        [Theory]
        [InlineData(0, 2, 2, 4)]
        [InlineData(1, 3, 3, 6)]
        [InlineData(0, 0xFFFF, -1, 2)]
        [InlineData(1, 4, -1, 3)]
        public void TestTableGrow(int tableidx, int n, int expected, int expected_new_size)
        {
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(2), ValueType.FUNCREF))
            );
            machine.SetTableAt(
                31,
                new Table("test", "$table1", new TableType(new Limits(3, 6), ValueType.FUNCREF))
            );

            // 0: REF_FUNC 10
            // 1: I32_CONST n
            // 2: TABLE_GROW tableidx
            // 3: NOP
            machine.SetProgram(
                0,
                Insn(InstructionType.REF_FUNC, new Value { s32 = 10 }), // Maps to addr 20
                I32Const(n),
                Insn(InstructionType.TABLE_GROW, new Value { s32 = tableidx }), // Maps to tableidx+30
                Nop()
            );

            machine.Step(3);

            Assert.Equal(expected, machine.Frame.TopOfStack.s32);
            Assert.Equal(expected_new_size, machine.tables[30 + tableidx].Elements.Length);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, 30, new int[5] { 10, 11, 12, 13, 14 })]
        [InlineData(0, 0, 0, 0, 2, 30, new int[5] { 30, 31, 12, 13, 14 })]
        [InlineData(0, 0, 0, 0, 3, 30, new int[5] { 30, 31, 32, 13, 14 })]
        [InlineData(0, 1, 0, 0, 3, 30, new int[5] { 31, 32, 33, 13, 14 })]
        [InlineData(1, 0, 0, 0, 3, 30, new int[5] { 40, 41, 42, 13, 14 })]
        [InlineData(0, 0, 0, 1, 3, 30, new int[5] { 10, 30, 31, 32, 14 })]
        [InlineData(0, 0, 1, 0, 3, 31, new int[5] { 30, 31, 32, 23, 24 })]
        public void TestTableInit(
            int s_elemidx,
            int s_offset,
            int d_tableidx,
            int d_offset,
            int n,
            int expected_tableaddr,
            int[] expected_content
        )
        {
            machine.SetElementSegmentAt(40, new ElementSegment(ValueType.FUNCREF, new Value[5]));
            machine.SetElementSegmentAt(41, new ElementSegment(ValueType.FUNCREF, new Value[5]));
            for (int i = 0; i < 5; i++)
            {
                machine.elementSegments[40].Elements[i] = Value.RefOfFuncAddr(30 + i);
                machine.elementSegments[41].Elements[i] = Value.RefOfFuncAddr(40 + i);
            }
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(5), ValueType.FUNCREF))
            );
            machine.SetTableAt(
                31,
                new Table("test", "$table1", new TableType(new Limits(5), ValueType.FUNCREF))
            );
            for (int i = 0; i < 5; i++)
            {
                machine.tables[30].Elements[i] = Value.RefOfFuncAddr(10 + i);
                machine.tables[31].Elements[i] = Value.RefOfFuncAddr(20 + i);
            }

            // 0: I32_CONST d_offset
            // 1: I32_CONST s_offset
            // 2: I32_CONST n
            // 3: TABLE_INIT d_tableidx, s_elemidx
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(d_offset),
                I32Const(s_offset),
                I32Const(n),
                Insn(
                    InstructionType.TABLE_INIT,
                    new Value { s32 = d_tableidx },
                    new Value { s32 = s_elemidx }
                ),
                Nop()
            );

            machine.Step(4);

            Assert.Equal(
                expected_content,
                machine.tables[expected_tableaddr].Elements.Select(e => e.RefAddr).ToArray()
            );
        }

        [Theory]
        [InlineData(0, 3, 0, 0, 3)]
        [InlineData(0, 0, 0, 3, 3)]
        public void TestTableInitTrapsOnOutOfBounds(
            int s_elemidx,
            int s_offset,
            int d_tableidx,
            int d_offset,
            int n
        )
        {
            machine.SetElementSegmentAt(40, new ElementSegment(ValueType.FUNCREF, new Value[5]));
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(5), ValueType.FUNCREF))
            );

            // 0: I32_CONST d_offset
            // 1: I32_CONST s_offset
            // 2: I32_CONST n
            // 3: TABLE_INIT d_tableidx, s_elemidx
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(d_offset),
                I32Const(s_offset),
                I32Const(n),
                Insn(
                    InstructionType.TABLE_INIT,
                    new Value { s32 = d_tableidx },
                    new Value { s32 = s_elemidx }
                ),
                Nop()
            );

            Assert.Throws<Trap>(() => machine.Step(4));
        }

        [Theory]
        [InlineData(0, 2, 2, 30, new int[5] { 10, 11, 50, 50, 14 })]
        [InlineData(0, 2, 0, 30, new int[5] { 10, 11, 12, 13, 14 })]
        [InlineData(1, 1, 2, 31, new int[5] { 20, 50, 50, 23, 24 })]
        public void TestTableFill(
            int tableidx,
            int offset,
            int n,
            int expected_tableaddr,
            int[] expected_content
        )
        {
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(5), ValueType.FUNCREF))
            );
            machine.SetTableAt(
                31,
                new Table("test", "$table1", new TableType(new Limits(5), ValueType.FUNCREF))
            );
            for (int i = 0; i < 5; i++)
            {
                machine.tables[30].Elements[i] = Value.RefOfFuncAddr(10 + i);
                machine.tables[31].Elements[i] = Value.RefOfFuncAddr(20 + i);
            }

            // 0: I32_CONST offset
            // 1: REF_FUNC 40
            // 2: I32_CONST n
            // 3: TABLE_FILL table_idx
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(InstructionType.REF_FUNC, new Value { s32 = 40 }), // Maps to addr 50.
                I32Const(n),
                Insn(InstructionType.TABLE_FILL, new Value { s32 = tableidx }),
                Nop()
            );

            machine.Step(4);

            Assert.Equal(
                expected_content,
                machine.tables[expected_tableaddr].Elements.Select(e => e.RefAddr).ToArray()
            );
        }

        [Theory]
        [InlineData(0, 5, 1)]
        [InlineData(0, 4, 2)]
        public void TestTableFillTrapsOnOutOfBounds(int tableidx, int offset, int n)
        {
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(5), ValueType.FUNCREF))
            );

            // 0: I32_CONST offset
            // 1: REF_FUNC 50
            // 2: I32_CONST n
            // 3: TABLE_FILL table_idx
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(InstructionType.REF_FUNC, Value.RefOfFuncAddr(50)),
                I32Const(n),
                Insn(InstructionType.TABLE_FILL, new Value { s32 = tableidx }),
                Nop()
            );

            Assert.Throws<Trap>(() => machine.Step(4));
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, 30, new int[5] { 10, 11, 12, 13, 14 })]
        [InlineData(0, 1, 0, 0, 3, 30, new int[5] { 11, 12, 13, 13, 14 })]
        [InlineData(0, 0, 0, 1, 3, 30, new int[5] { 10, 10, 11, 12, 14 })]
        [InlineData(1, 0, 0, 0, 3, 30, new int[5] { 20, 21, 22, 13, 14 })]
        [InlineData(0, 0, 1, 0, 3, 31, new int[5] { 10, 11, 12, 23, 24 })]
        public void TestTableCopy(
            int s_tableidx,
            int s_offset,
            int d_tableidx,
            int d_offset,
            int n,
            int expected_tableaddr,
            int[] expected_content
        )
        {
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(5), ValueType.FUNCREF))
            );
            machine.SetTableAt(
                31,
                new Table("test", "$table1", new TableType(new Limits(5), ValueType.FUNCREF))
            );
            for (int i = 0; i < 5; i++)
            {
                machine.tables[30].Elements[i] = Value.RefOfFuncAddr(10 + i);
                machine.tables[31].Elements[i] = Value.RefOfFuncAddr(20 + i);
            }

            // 0: I32_CONST d_offset
            // 1: I32_CONST s_offset
            // 2: I32_CONST n
            // 3: TABLE_COPY d_tableidx, s_tableidx
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(d_offset),
                I32Const(s_offset),
                I32Const(n),
                Insn(
                    InstructionType.TABLE_COPY,
                    new Value { s32 = d_tableidx },
                    new Value { s32 = s_tableidx }
                ),
                Nop()
            );

            machine.Step(4);

            Assert.Equal(
                expected_content,
                machine.tables[expected_tableaddr].Elements.Select(e => e.RefAddr).ToArray()
            );
        }

        [Theory]
        [InlineData(0, 5, 0, 0, 1)]
        [InlineData(0, 0, 0, 4, 2)]
        public void TestTableCopyTrapsOnOutOfBounds(
            int s_tableidx,
            int s_offset,
            int d_tableidx,
            int d_offset,
            int n
        )
        {
            machine.SetTableAt(
                30,
                new Table("test", "$table0", new TableType(new Limits(5), ValueType.FUNCREF))
            );

            // 0: I32_CONST d_offset
            // 1: I32_CONST s_offset
            // 2: I32_CONST n
            // 3: TABLE_COPY d_tableidx, s_tableidx
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(d_offset),
                I32Const(s_offset),
                I32Const(n),
                Insn(
                    InstructionType.TABLE_COPY,
                    new Value { s32 = d_tableidx },
                    new Value { s32 = s_tableidx }
                ),
                Nop()
            );

            Assert.Throws<Trap>(() => machine.Step(4));
        }

        [Theory]
        [InlineData(0, 40)]
        [InlineData(1, 41)]
        public void TestElemDrop(int elemidx, int expected_elem_segment)
        {
            machine.SetElementSegmentAt(40, new ElementSegment(ValueType.FUNCREF, new Value[5]));
            machine.SetElementSegmentAt(41, new ElementSegment(ValueType.FUNCREF, new Value[5]));

            // 0: ELEM_DROP elemidx
            // 1: NOP
            machine.SetProgram(
                0,
                Insn(InstructionType.ELEM_DROP, new Value { s32 = elemidx }),
                Nop()
            );

            machine.Step();

            Assert.Null(machine.elementSegments[expected_elem_segment]);
        }
    }
}
