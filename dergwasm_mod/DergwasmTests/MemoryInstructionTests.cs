using System;
using System.Linq;
using Derg;
using Xunit;

namespace DergwasmTests
{
    public class MemoryInstructionTests : InstructionTestFixture
    {
        [Theory]
        [InlineData(InstructionType.I32_LOAD, 0, 0, 0x1234A6A8U)]
        [InlineData(InstructionType.I32_LOAD, 0, 1, 0xFF1234A6U)]
        [InlineData(InstructionType.I32_LOAD, 1, 0, 0xFF1234A6U)]
        [InlineData(InstructionType.I32_LOAD8_S, 0, 0, 0xFFFFFFA8)]
        [InlineData(InstructionType.I32_LOAD8_U, 0, 0, 0xA8)]
        [InlineData(InstructionType.I32_LOAD16_S, 0, 0, 0xFFFFA6A8)]
        [InlineData(InstructionType.I32_LOAD16_U, 0, 0, 0xA6A8)]
        public void TestI32Load(InstructionType insn, int offset, int base_addr, uint expected)
        {
            Array.Copy(new byte[] { 0xA8, 0xA6, 0x34, 0x12, 0xFF }, machine.Heap, 5);

            // 0: I32_CONST offset
            // 1: I32_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(insn, new Value { s32 = 0 }, new Value { s32 = base_addr }),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(expected, machine.Frame.TopOfStack.u32);
        }

        [Theory]
        [InlineData(InstructionType.I32_LOAD, 0xFFFF, 0)]
        [InlineData(InstructionType.I32_LOAD, 0, 0xFFFF)]
        [InlineData(InstructionType.I64_LOAD, 0xFFFF, 0)]
        [InlineData(InstructionType.I64_LOAD, 0, 0xFFFF)]
        [InlineData(InstructionType.F32_LOAD, 0xFFFF, 0)]
        [InlineData(InstructionType.F32_LOAD, 0, 0xFFFF)]
        [InlineData(InstructionType.F64_LOAD, 0xFFFF, 0)]
        [InlineData(InstructionType.F64_LOAD, 0, 0xFFFF)]
        public void TestLoadTrapsOnOutOfBounds(InstructionType insn, int offset, int base_addr)
        {
            // 0: I32_CONST offset
            // 1: I32_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(insn, new Value { s32 = 0 }, new Value { s32 = base_addr }),
                Nop()
            );

            Assert.Throws<Trap>(() => machine.Step(2));
        }

        [Theory]
        [InlineData(InstructionType.I64_LOAD, 0, 0, 0x08070605A4A3A2A1UL)]
        [InlineData(InstructionType.I64_LOAD, 0, 1, 0x0908070605A4A3A2UL)]
        [InlineData(InstructionType.I64_LOAD, 1, 0, 0x0908070605A4A3A2UL)]
        [InlineData(InstructionType.I64_LOAD8_S, 0, 0, 0xFFFFFFFFFFFFFFA1UL)]
        [InlineData(InstructionType.I64_LOAD8_U, 0, 0, 0x00000000000000A1UL)]
        [InlineData(InstructionType.I64_LOAD16_S, 0, 0, 0xFFFFFFFFFFFFA2A1UL)]
        [InlineData(InstructionType.I64_LOAD16_U, 0, 0, 0x000000000000A2A1UL)]
        [InlineData(InstructionType.I64_LOAD32_S, 0, 0, 0xFFFFFFFFA4A3A2A1UL)]
        [InlineData(InstructionType.I64_LOAD32_U, 0, 0, 0x00000000A4A3A2A1UL)]
        public void TestI64Load(InstructionType insn, int offset, int base_addr, ulong expected)
        {
            Array.Copy(new byte[] { 0xA1, 0xA2, 0xA3, 0xA4, 5, 6, 7, 8, 9 }, machine.Heap, 9);

            // 0: I32_CONST offset
            // 1: I64_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(insn, new Value { s32 = 0 }, new Value { s32 = base_addr }),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(expected, machine.Frame.TopOfStack.u64);
        }

        [Theory]
        [InlineData(0, 0, 5.69045661E-28f)]
        [InlineData(0, 1, -1.94339031E+38f)]
        [InlineData(1, 0, -1.94339031E+38f)]
        public void TestF32Load(int offset, int base_addr, float expected)
        {
            Array.Copy(new byte[] { 0x78, 0x56, 0x34, 0x12, 0xFF }, machine.Heap, 5);

            // 0: I32_CONST offset
            // 1: F32_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(
                    InstructionType.F32_LOAD,
                    new Value { s32 = 0 },
                    new Value { s32 = base_addr }
                ),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(expected, machine.Frame.TopOfStack.f32);
        }

        [Theory]
        [InlineData(0, 0, 5.447603722011605E-270)]
        [InlineData(0, 1, 3.7258146895053074E-265)]
        [InlineData(1, 0, 3.7258146895053074E-265)]
        public void TestF64Load(int offset, int base_addr, double expected)
        {
            Array.Copy(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, machine.Heap, 9);

            // 0: I32_CONST offset
            // 1: F64_LOAD _ base_addr
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                Insn(
                    InstructionType.F64_LOAD,
                    new Value { s32 = 0 },
                    new Value { s32 = base_addr }
                ),
                Nop()
            );

            machine.Step(2);

            Assert.Equal(expected, machine.Frame.TopOfStack.f64);
        }

        [Theory]
        [InlineData(InstructionType.I32_STORE, 0, 0, 0x1234A6A8U, 0x000000001234A6A8UL)]
        [InlineData(InstructionType.I32_STORE, 1, 0, 0x1234A6A8U, 0x0000001234A6A800UL)]
        [InlineData(InstructionType.I32_STORE, 0, 1, 0x1234A6A8U, 0x0000001234A6A800UL)]
        [InlineData(InstructionType.I32_STORE8, 0, 0, 0xFFFFFFA8U, 0x00000000000000A8UL)]
        [InlineData(InstructionType.I32_STORE16, 0, 0, 0xFFFFA6A8U, 0x000000000000A6A8UL)]
        public void TestI32Store(
            InstructionType insn,
            int offset,
            int base_addr,
            uint val,
            ulong expected
        )
        {
            // 0: I32_CONST offset
            // 1: I32_CONST val
            // 2: I32_STORE _ base_addr
            // 3: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                I32Const(val),
                Insn(insn, new Value { s32 = 0 }, new Value { s32 = base_addr }),
                Nop()
            );

            machine.Step(3);

            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(expected, MemoryInstructions.Convert<ulong>(machine.Heap, 0));
        }

        [Theory]
        [InlineData(0, 0, 0x000000004048F5C3UL)]
        [InlineData(1, 0, 0x0000004048F5C300UL)]
        [InlineData(0, 1, 0x0000004048F5C300UL)]
        public void TestF32Store(int offset, int base_addr, ulong expected)
        {
            // 0: I32_CONST offset
            // 1: F32_CONST 3.14 // 0x4048F5C3
            // 2: F32_STORE _ base_addr
            // 3: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                F32Const(3.14f),
                Insn(
                    InstructionType.F32_STORE,
                    new Value { s32 = 0 },
                    new Value { s32 = base_addr }
                ),
                Nop()
            );

            machine.Step(3);

            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(expected, MemoryInstructions.Convert<ulong>(machine.Heap, 0));
        }

        [Theory]
        [InlineData(
            InstructionType.I64_STORE,
            0,
            0,
            0xA1A2A3A4A5A6A7A8UL,
            0xA1A2A3A4A5A6A7A8UL,
            0x00
        )]
        [InlineData(
            InstructionType.I64_STORE,
            1,
            0,
            0xA1A2A3A4A5A6A7A8UL,
            0xA2A3A4A5A6A7A800UL,
            0xA1
        )]
        [InlineData(
            InstructionType.I64_STORE,
            0,
            1,
            0xA1A2A3A4A5A6A7A8UL,
            0xA2A3A4A5A6A7A800UL,
            0xA1
        )]
        [InlineData(
            InstructionType.I64_STORE8,
            0,
            0,
            0xFFFFFFFFFFFFFFA1UL,
            0x00000000000000A1UL,
            0x00
        )]
        [InlineData(
            InstructionType.I64_STORE16,
            0,
            0,
            0xFFFFFFFFFFFFA1A2UL,
            0x000000000000A1A2UL,
            0x00
        )]
        [InlineData(
            InstructionType.I64_STORE32,
            0,
            0,
            0xFFFFFFFFA1A2A3A4UL,
            0x00000000A1A2A3A4UL,
            0x00
        )]
        public void TestI64Store(
            InstructionType insn,
            int offset,
            int base_addr,
            ulong val,
            ulong expected,
            byte expected8
        )
        {
            // 0: I32_CONST offset
            // 1: I64_CONST val
            // 2: I64_STORE _ base_addr
            // 3: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                I64Const(val),
                Insn(insn, new Value { s32 = 0 }, new Value { s32 = base_addr }),
                Nop()
            );

            machine.Step(3);

            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(expected, MemoryInstructions.Convert<ulong>(machine.Heap, 0));
            Assert.Equal(expected8, machine.Heap[8]);
        }

        [Theory]
        [InlineData(0, 0, 0x40091EB851EB851FUL, 0x00)]
        [InlineData(1, 0, 0x091EB851EB851F00UL, 0x40)]
        [InlineData(0, 1, 0x091EB851EB851F00UL, 0x40)]
        public void TestF64Store(int offset, int base_addr, ulong expected, byte expected8)
        {
            // 0: I32_CONST offset
            // 1: F64_CONST 3.14 // 0x40091EB851EB851F
            // 2: F64_STORE _ base_addr
            // 3: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                F64Const(3.14),
                Insn(
                    InstructionType.F64_STORE,
                    new Value { s32 = 0 },
                    new Value { s32 = base_addr }
                ),
                Nop()
            );

            machine.Step(3);

            Assert.Empty(machine.Frame.value_stack);
            Assert.Equal(expected, MemoryInstructions.Convert<ulong>(machine.Heap, 0));
            Assert.Equal(expected8, machine.Heap[8]);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void TestMemorySize(int sz)
        {
            machine.memories[0].Data = new byte[sz << 16];

            // 0: MEMORY_SIZE 0
            // 1: NOP
            machine.SetProgram(0, Insn(InstructionType.MEMORY_SIZE, new Value { s32 = 0 }), Nop());

            machine.Step();

            Assert.Collection(machine.Frame.value_stack, v => Assert.Equal(sz, v.s32));
        }

        [Theory]
        [InlineData(3, 0, 1, 0x10000)]
        [InlineData(3, 1, 1, 0x20000)]
        [InlineData(3, 3, -1, 0x10000)]
        [InlineData(0xFFFF, 0x7FFF, -1, 0x10000)]
        public void TestMemoryGrow(
            uint max_limit,
            int delta,
            int expected_return,
            int expected_size
        )
        {
            machine.memories[0].Limits.Maximum = max_limit;

            // 0: I32_CONST delta
            // 1: MEMORY_GROW 0
            // 2: NOP
            machine.SetProgram(
                0,
                I32Const(delta),
                Insn(InstructionType.MEMORY_GROW, new Value { s32 = 0 }),
                Nop()
            );

            machine.Step(2);

            Assert.Collection(machine.Frame.value_stack, v => Assert.Equal(expected_return, v.s32));

            Assert.Equal(expected_size, machine.Heap.Length);
        }

        [Theory]
        [InlineData(0, 0, 0xFF10, new byte[] { 0, 0, 0, 0, 0 })]
        [InlineData(0, 1, 0xFF10, new byte[] { 0x10, 0, 0, 0, 0 })]
        [InlineData(0, 1, 0xFF11, new byte[] { 0x11, 0, 0, 0, 0 })]
        [InlineData(1, 1, 0xFF10, new byte[] { 0, 0x10, 0, 0, 0 })]
        [InlineData(1, 2, 0xFF10, new byte[] { 0, 0x10, 0x10, 0, 0 })]
        public void TestMemoryFill(int offset, int sz, int value, byte[] expected)
        {
            // 0: I32_CONST offset
            // 1: I32_CONST value
            // 2: I32_CONST sz
            // 3: MEMORY_FILL 0
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                I32Const(value),
                I32Const(sz),
                Insn(InstructionType.MEMORY_FILL, new Value { s32 = 0 }),
                Nop()
            );

            machine.Step(4);

            Assert.Empty(machine.Frame.value_stack);

            Assert.Equal(expected, new ArraySegment<byte>(machine.Heap, 0, 5).ToArray());
        }

        [Theory]
        [InlineData(0, 0x10001)]
        [InlineData(0x10000, 1)]
        public void TestMemoryFillTrapsOnOutOfBounds(int offset, int sz)
        {
            // 0: I32_CONST offset
            // 1: I32_CONST value
            // 2: I32_CONST sz
            // 3: MEMORY_FILL 0
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(offset),
                I32Const(0),
                I32Const(sz),
                Insn(InstructionType.MEMORY_FILL, new Value { s32 = 0 }),
                Nop()
            );

            Assert.Throws<Trap>(() => machine.Step(4));
        }

        [Theory]
        [InlineData(0, 0, 0, new byte[] { 1, 2, 3, 4, 5 })]
        [InlineData(0, 1, 3, new byte[] { 1, 1, 2, 3, 5 })]
        [InlineData(1, 0, 3, new byte[] { 2, 3, 4, 4, 5 })]
        public void TestMemoryCopy(int s_offset, int d_offset, int sz, byte[] expected)
        {
            Array.Copy(new byte[] { 1, 2, 3, 4, 5 }, machine.Heap, 5);

            // 0: I32_CONST d_offset
            // 1: I32_CONST s_offset
            // 2: I32_CONST sz
            // 3: MEMORY_COPY 0
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(d_offset),
                I32Const(s_offset),
                I32Const(sz),
                Insn(InstructionType.MEMORY_COPY, new Value { s32 = 0 }),
                Nop()
            );

            machine.Step(4);

            Assert.Empty(machine.Frame.value_stack);

            Assert.Equal(expected, new ArraySegment<byte>(machine.Heap, 0, 5).ToArray());
        }

        [Theory]
        [InlineData(0x10000, 0, 1)]
        [InlineData(0, 0x10000, 1)]
        public void TestMemoryCopyTrapsOnOutOfBounds(int s_offset, int d_offset, int sz)
        {
            // 0: I32_CONST d_offset
            // 1: I32_CONST s_offset
            // 2: I32_CONST sz
            // 3: MEMORY_FILL 0
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(d_offset),
                I32Const(s_offset),
                I32Const(sz),
                Insn(InstructionType.MEMORY_COPY, new Value { s32 = 0 }),
                Nop()
            );

            Assert.Throws<Trap>(() => machine.Step(4));
        }

        [Theory]
        [InlineData(0, 0, 0, 0, new byte[] { 0, 0, 0, 0, 0 })]
        [InlineData(0, 0, 0, 2, new byte[] { 1, 2, 0, 0, 0 })]
        [InlineData(0, 1, 0, 2, new byte[] { 2, 3, 0, 0, 0 })]
        [InlineData(0, 0, 1, 2, new byte[] { 0, 1, 2, 0, 0 })]
        [InlineData(1, 0, 0, 2, new byte[] { 6, 7, 0, 0, 0 })]
        public void TestMemoryInit(int dataidx, int s_offset, int d_offset, int sz, byte[] expected)
        {
            machine.SetDataSegmentAt(50, new byte[] { 1, 2, 3, 4, 5 });
            machine.SetDataSegmentAt(51, new byte[] { 6, 7 });

            // 0: I32_CONST d_offset
            // 1: I32_CONST s_offset
            // 2: I32_CONST sz
            // 3: MEMORY_INIT dataidx 0
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(d_offset),
                I32Const(s_offset),
                I32Const(sz),
                Insn(
                    InstructionType.MEMORY_INIT,
                    new Value { s32 = dataidx },
                    new Value { s32 = 0 }
                ),
                Nop()
            );

            machine.Step(4);

            Assert.Empty(machine.Frame.value_stack);

            Assert.Equal(expected, new ArraySegment<byte>(machine.Heap, 0, 5).ToArray());
        }

        [Theory]
        [InlineData(0, 5, 0, 1)]
        [InlineData(0, 3, 0, 3)]
        [InlineData(1, 0, 0, 3)]
        [InlineData(0, 0, 0x10000, 1)]
        public void TestMemoryInitTrapsOnOutOfBounds(
            int dataidx,
            int s_offset,
            int d_offset,
            int sz
        )
        {
            machine.SetDataSegmentAt(50, new byte[] { 1, 2, 3, 4, 5 });
            machine.SetDataSegmentAt(51, new byte[] { 6, 7 });

            // 0: I32_CONST d_offset
            // 1: I32_CONST s_offset
            // 2: I32_CONST sz
            // 3: MEMORY_INIT dataidx 0
            // 4: NOP
            machine.SetProgram(
                0,
                I32Const(d_offset),
                I32Const(s_offset),
                I32Const(sz),
                Insn(
                    InstructionType.MEMORY_INIT,
                    new Value { s32 = dataidx },
                    new Value { s32 = 0 }
                ),
                Nop()
            );

            Assert.Throws<Trap>(() => machine.Step(4));
        }

        [Theory]
        [InlineData(0, 50)]
        [InlineData(1, 51)]
        public void TestDataDrop(int dataidx, int expected_addr)
        {
            machine.SetDataSegmentAt(50, new byte[] { 1, 2, 3, 4, 5 });
            machine.SetDataSegmentAt(51, new byte[] { 6, 7 });

            // 0: DATA_DROP dataidx
            // 1: NOP
            machine.SetProgram(
                0,
                Insn(InstructionType.DATA_DROP, new Value { s32 = dataidx }),
                Nop()
            );

            machine.Step();

            Assert.Empty(machine.Frame.value_stack);
            Assert.Null(machine.dataSegments[expected_addr]);
        }
    }
}
