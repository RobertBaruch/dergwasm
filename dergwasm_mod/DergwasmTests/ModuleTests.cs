using System.IO;
using System.Text;
using Derg;
using LEB128;
using Xunit;

namespace DergwasmTests
{
    public class ModuleTests
    {
        static void WriteString(BinaryWriter writer, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            writer.WriteLEB128Unsigned((ulong)bytes.Length);
            writer.Write(bytes);
        }

        static void WriteTestFuncType(BinaryWriter writer)
        {
            writer.Write((byte)0x60); // FuncType tag
            writer.WriteLEB128Unsigned(2UL); // 2 args
            writer.Write((byte)ValueType.I32);
            writer.Write((byte)ValueType.I64);
            writer.WriteLEB128Unsigned(2UL); // 2 returns
            writer.Write((byte)ValueType.F32);
            writer.Write((byte)ValueType.F64);
        }

        static FuncType TestFuncType0 = new FuncType(
            new ValueType[] { ValueType.I64 },
            new ValueType[0]
        );
        static FuncType TestFuncType1 = new FuncType(
            new ValueType[] { ValueType.I32, ValueType.I64 },
            new ValueType[] { ValueType.F32, ValueType.F64 }
        );

        static void WriteTestTypesSection(BinaryWriter writer)
        {
            writer.Write((byte)1); // Type section

            MemoryStream sectionStream = new MemoryStream();
            BinaryWriter sectionWriter = new BinaryWriter(sectionStream);
            sectionWriter.WriteLEB128Unsigned(2UL); // 2 FuncTypes

            sectionWriter.Write((byte)0x60); // FuncType tag
            sectionWriter.WriteLEB128Unsigned(1UL); // 1 arg
            sectionWriter.Write((byte)ValueType.I64);
            sectionWriter.WriteLEB128Unsigned(0UL); // 0 returns

            sectionWriter.Write((byte)0x60); // FuncType tag
            sectionWriter.WriteLEB128Unsigned(2UL); // 2 args
            sectionWriter.Write((byte)ValueType.I32);
            sectionWriter.Write((byte)ValueType.I64);
            sectionWriter.WriteLEB128Unsigned(2UL); // 2 returns
            sectionWriter.Write((byte)ValueType.F32);
            sectionWriter.Write((byte)ValueType.F64);

            writer.WriteLEB128Unsigned((ulong)sectionStream.Length);
            writer.Write(sectionStream.ToArray());
        }

        static TableType TestTableType0 = new TableType(new Limits(1), ValueType.FUNCREF);

        static void WriteTestTableType(BinaryWriter writer)
        {
            writer.Write((byte)ValueType.FUNCREF);
            writer.Write((byte)0); // No maximum
            writer.WriteLEB128Unsigned(1UL); // Minimum 1
        }

        static Limits TestMemoryType0 = new Limits(1, 10);

        static void WriteTestMemoryType(BinaryWriter writer)
        {
            writer.Write((byte)1); // Has maximum
            writer.WriteLEB128Unsigned(1UL); // Minimum 1
            writer.WriteLEB128Unsigned(10UL); // Maximum 10
        }

        static GlobalType TestGlobalType0 = new GlobalType(ValueType.I32, true);

        static void WriteTestGlobalType(BinaryWriter writer)
        {
            writer.Write((byte)ValueType.I32);
            writer.Write((byte)1); // Mutable
        }

        [Fact]
        public void ReadEmptyModuleWorks()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write(0x6D736100U);
            writer.Write(1U);
            memStream.Position = 0;
            BinaryReader reader = new BinaryReader(memStream);

            Module module = Module.Read(reader);

            Assert.Empty(module.customData);
            Assert.Empty(module.FuncTypes);
            Assert.Empty(module.Imports);
            Assert.Empty(module.Exports);
            Assert.Empty(module.Funcs);
            Assert.Empty(module.Tables);
            Assert.Empty(module.Memories);
            Assert.Empty(module.Globals);
            Assert.Empty(module.ElementSegmentSpecs);
            Assert.Empty(module.DataSegments);
            Assert.Equal(-1, module.StartIdx);
            Assert.Equal(0, module.DataCount);
        }

        [Fact]
        public void ReadModuleTrapsOnBadMagic()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write(0x6D736101U);
            writer.Write(1U);
            memStream.Position = 0;
            BinaryReader reader = new BinaryReader(memStream);

            Assert.Throws<Trap>(() => Module.Read(reader));
        }

        [Fact]
        public void ReadModuleTrapsOnBadVersion()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write(0x6D736100U);
            writer.Write(2U);
            memStream.Position = 0;
            BinaryReader reader = new BinaryReader(memStream);

            Assert.Throws<Trap>(() => Module.Read(reader));
        }

        [Fact]
        public void ReadsCustomSectionCorrectly()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write(0x6D736100U);
            writer.Write(1U);
            writer.Write((byte)0); // Custom section

            MemoryStream sectionStream = new MemoryStream();
            BinaryWriter sectionWriter = new BinaryWriter(sectionStream);
            WriteString(sectionWriter, "custom section");
            sectionWriter.WriteLEB128Unsigned(4UL);
            sectionWriter.Write(0xF100FF1EU);
            sectionStream.Position = 0;

            writer.WriteLEB128Unsigned((ulong)sectionStream.Length);
            writer.Write(sectionStream.ToArray());
            memStream.Position = 0;
            BinaryReader reader = new BinaryReader(memStream);

            Module module = Module.Read(reader);

            Assert.Single(module.customData);
            Assert.Collection(module.customData, e => Assert.Equal("custom section", e.Name));
            Assert.Collection(
                module.customData,
                e => Assert.Equal(new byte[] { 0x1E, 0xFF, 0x00, 0xF1 }, e.Data)
            );
        }

        [Fact]
        public void ReadsFuncTypeCorrectly()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            WriteTestFuncType(writer);
            memStream.Position = 0;

            BinaryReader reader = new BinaryReader(memStream);

            FuncType funcType = FuncType.Read(reader);

            Assert.Collection(
                funcType.args,
                e => Assert.Equal(ValueType.I32, e),
                e => Assert.Equal(ValueType.I64, e)
            );
            Assert.Collection(
                funcType.returns,
                e => Assert.Equal(ValueType.F32, e),
                e => Assert.Equal(ValueType.F64, e)
            );
        }

        [Fact]
        public void ReadFuncTypeTrapsOnBadTag()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write((byte)0x7F);
            memStream.Position = 0;

            BinaryReader reader = new BinaryReader(memStream);

            Assert.Throws<Trap>(() => FuncType.Read(reader));
        }

        [Fact]
        public void ReadsNoMaximumLimitsCorrectly()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write((byte)0); // No maximum
            writer.WriteLEB128Unsigned(1UL); // Minimum 1
            memStream.Position = 0;

            BinaryReader reader = new BinaryReader(memStream);

            Limits limits = Limits.Read(reader);

            Assert.Equal(1U, limits.Minimum);
            Assert.False(limits.Maximum.HasValue);
        }

        [Fact]
        public void ReadsMaximumLimitsCorrectly()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write((byte)1); // Has maximum
            writer.WriteLEB128Unsigned(1UL); // Minimum 1
            writer.WriteLEB128Unsigned(2UL); // Maximum 2
            memStream.Position = 0;

            BinaryReader reader = new BinaryReader(memStream);

            Limits limits = Limits.Read(reader);

            Assert.Equal(1U, limits.Minimum);
            Assert.True(limits.Maximum.HasValue);
            Assert.Equal(2U, limits.Maximum.Value);
        }

        [Fact]
        public void ReadLimitsTrapsOnBadFlag()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write((byte)2); // Bad flag
            memStream.Position = 0;

            BinaryReader reader = new BinaryReader(memStream);

            Assert.Throws<Trap>(() => Limits.Read(reader));
        }

        [Fact]
        public void ReadTableTypeCorrectly()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write((byte)ValueType.FUNCREF);
            writer.Write((byte)0); // No maximum
            writer.WriteLEB128Unsigned(1UL); // Minimum 1
            memStream.Position = 0;

            BinaryReader reader = new BinaryReader(memStream);

            TableType tableType = TableType.Read(reader);

            Assert.Equal(ValueType.FUNCREF, tableType.ElementType);
            Assert.Equal(1U, tableType.Limits.Minimum);
            Assert.False(tableType.Limits.Maximum.HasValue);
        }

        [Fact]
        public void ReadTableTypeTrapsOnBadElementType()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write((byte)ValueType.I32); // Bad element type
            writer.Write((byte)0); // No maximum
            writer.WriteLEB128Unsigned(1UL); // Minimum 1
            memStream.Position = 0;

            BinaryReader reader = new BinaryReader(memStream);

            Assert.Throws<Trap>(() => TableType.Read(reader));
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        public void ReadsGlobalTypeCorrectly(byte mutableFlag, bool expected)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write((byte)ValueType.I32);
            writer.Write(mutableFlag);
            memStream.Position = 0;

            BinaryReader reader = new BinaryReader(memStream);

            GlobalType globalType = GlobalType.Read(reader);

            Assert.Equal(ValueType.I32, globalType.Type);
            Assert.Equal(expected, globalType.Mutable);
        }

        [Fact]
        public void ReadsTypeSectionCorrectly()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write(0x6D736100U);
            writer.Write(1U);
            writer.Write((byte)1); // Type section

            MemoryStream sectionStream = new MemoryStream();
            BinaryWriter sectionWriter = new BinaryWriter(sectionStream);
            sectionWriter.WriteLEB128Unsigned(2UL); // 2 FuncTypes

            sectionWriter.Write((byte)0x60); // FuncType tag
            sectionWriter.WriteLEB128Unsigned(1UL); // 1 arg
            sectionWriter.Write((byte)ValueType.I64);
            sectionWriter.WriteLEB128Unsigned(0UL); // 0 returns
            WriteTestFuncType(sectionWriter);

            writer.WriteLEB128Unsigned((ulong)sectionStream.Length);
            writer.Write(sectionStream.ToArray());

            memStream.Position = 0;
            BinaryReader reader = new BinaryReader(memStream);

            Module module = Module.Read(reader);

            Assert.Collection(
                module.FuncTypes,
                e =>
                    Assert.Equal(
                        new FuncType(new ValueType[] { ValueType.I64 }, new ValueType[0]),
                        e
                    ),
                e =>
                    Assert.Equal(
                        new FuncType(
                            new ValueType[] { ValueType.I32, ValueType.I64 },
                            new ValueType[] { ValueType.F32, ValueType.F64 }
                        ),
                        e
                    )
            );
        }

        [Fact]
        public void ReadsImportSectionCorrectly()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write(0x6D736100U);
            writer.Write(1U);
            WriteTestTypesSection(writer);

            writer.Write((byte)2); // Import section

            MemoryStream sectionStream = new MemoryStream();
            BinaryWriter sectionWriter = new BinaryWriter(sectionStream);
            sectionWriter.WriteLEB128Unsigned(5UL); // 5 imports

            WriteString(sectionWriter, "module1");
            WriteString(sectionWriter, "func1");
            sectionWriter.Write((byte)0); // Func import
            sectionWriter.WriteLEB128Unsigned(0UL); // Type index 0

            WriteString(sectionWriter, "module2");
            WriteString(sectionWriter, "func2");
            sectionWriter.Write((byte)0); // Func import
            sectionWriter.WriteLEB128Unsigned(1UL); // Type index 1

            WriteString(sectionWriter, "module3");
            WriteString(sectionWriter, "table1");
            sectionWriter.Write((byte)1); // Table import
            WriteTestTableType(sectionWriter);

            WriteString(sectionWriter, "module4");
            WriteString(sectionWriter, "memory1");
            sectionWriter.Write((byte)2); // Memory import
            WriteTestMemoryType(sectionWriter);

            WriteString(sectionWriter, "module5");
            WriteString(sectionWriter, "global1");
            sectionWriter.Write((byte)3); // Global import
            WriteTestGlobalType(sectionWriter);

            writer.WriteLEB128Unsigned((ulong)sectionStream.Length);
            writer.Write(sectionStream.ToArray());

            memStream.Position = 0;
            BinaryReader reader = new BinaryReader(memStream);

            Module module = Module.Read(reader);

            Assert.Collection(
                module.Imports,
                e =>
                {
                    Assert.Equal("module1", e.ModuleName);
                    Assert.Equal("func1", e.Name);
                    Assert.IsType<FuncImport>(e);
                    Assert.Equal(TestFuncType0, (e as FuncImport).FuncType);
                },
                e =>
                {
                    Assert.Equal("module2", e.ModuleName);
                    Assert.Equal("func2", e.Name);
                    Assert.IsType<FuncImport>(e);
                    Assert.Equal(TestFuncType1, (e as FuncImport).FuncType);
                },
                e =>
                {
                    Assert.Equal("module3", e.ModuleName);
                    Assert.Equal("table1", e.Name);
                    Assert.IsType<TableImport>(e);
                    Assert.Equal(TestTableType0, (e as TableImport).TableType);
                },
                e =>
                {
                    Assert.Equal("module4", e.ModuleName);
                    Assert.Equal("memory1", e.Name);
                    Assert.IsType<MemoryImport>(e);
                    Assert.Equal(TestMemoryType0, (e as MemoryImport).MemoryLimits);
                },
                e =>
                {
                    Assert.Equal("module5", e.ModuleName);
                    Assert.Equal("global1", e.Name);
                    Assert.IsType<GlobalImport>(e);
                    Assert.Equal(TestGlobalType0, (e as GlobalImport).GlobalType);
                }
            );
        }
    }
}
