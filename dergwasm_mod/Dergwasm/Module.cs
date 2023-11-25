using FrooxEngine;
using FrooxEngine.Undo;
using LEB128;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    // The file representation of a module.
    public class Module
    {
        public static readonly uint Magic = 0x6D736100U;
        public static readonly uint Version = 1U;

        public string ModuleName;
        public List<CustomData> customData = new List<CustomData>();
        public FuncType[] FuncTypes = new FuncType[0];
        public Import[] Imports = new Import[0];
        public Export[] Exports = new Export[0];
        public List<Func> Funcs = new List<Func>(); // Includes imported functions at the beginning.
        public List<TableType> Tables = new List<TableType>(); // Includes imported tables at the beginning.
        public List<Limits> Memories = new List<Limits>(); // Includes imported memories at the beginning.
        public List<GlobalSpec> Globals = new List<GlobalSpec>(); // Includes imported globals at the beginning.
        public ElementSegmentSpec[] ElementSegmentSpecs = new ElementSegmentSpec[0];
        public DataSegment[] DataSegments = new DataSegment[0];
        public int StartIdx = -1;
        public int DataCount;

        public int[] ExternalFuncAddrs;
        public int[] ExternalTableAddrs;
        public int[] ExternalMemoryAddrs;
        public int[] ExternalGlobalAddrs;

        public Module(string moduleName)
        {
            moduleName = moduleName;
        }

        public static Module Read(string moduleName, BinaryReader stream)
        {
            if (stream.ReadUInt32() != Magic)
            {
                throw new Trap("Invalid magic number");
            }
            if (stream.ReadUInt32() != Version)
            {
                throw new Trap("Invalid version");
            }

            Module module = new Module(moduleName);

            while (true)
            {
                byte section_id;
                try
                {
                    section_id = stream.ReadByte();
                }
                catch (EndOfStreamException)
                {
                    break;
                }
                int section_len = (int)stream.ReadLEB128Unsigned();
                Section.SectionReaders[section_id](stream, module);
            }

            return module;
        }

        public static List<Instruction> ReadExpr(BinaryReader stream) =>
            Expr.Decode(stream).Flatten(0);

        // You should resolve externs for all modules before instantiating any of them. This only
        // matches names. The func types will be validated during instantiation.
        public void ResolveExterns(IMachine machine)
        {
            ResolveExternFuncs(machine);
            ResolveExternTables(machine);
            ResolveExternMemories(machine);
            ResolveExternGlobals(machine);
        }

        void ResolveExternFuncs(IMachine machine)
        {
            ExternalFuncAddrs = new int[NumImportedFuncs()];

            // Match imported functions to their machine addresses.
            for (int i = 0; i < NumImportedFuncs(); i++)
            {
                ImportedFunc importedFunc = (ImportedFunc)Funcs[i];
                Func matchedFunc = null;

                // O(N) for now. Ideally the module name and func name would be in a dictionary.
                for (int addr = 0; addr < machine.NumFuncs; addr++)
                {
                    Func func = machine.GetFunc(addr);
                    if (
                        func.ModuleName == importedFunc.ModuleName && func.Name == importedFunc.Name
                    )
                    {
                        matchedFunc = func;
                        ExternalFuncAddrs[i] = addr;
                        break;
                    }
                }

                if (matchedFunc == null)
                {
                    throw new Trap(
                        $"Could not resolve imported function {importedFunc.ModuleName}.{importedFunc.Name}"
                    );
                }
            }
        }

        void ResolveExternTables(IMachine machine)
        {
            ExternalTableAddrs = new int[0];
        }

        void ResolveExternMemories(IMachine machine)
        {
            ExternalMemoryAddrs = new int[0];
        }

        void ResolveExternGlobals(IMachine machine)
        {
            ExternalGlobalAddrs = new int[0];
        }

        public ModuleInstance Instantiate(IMachine machine)
        {
            ModuleInstance instance = new ModuleInstance();
            instance.Instantiate(machine, this);
            return instance;
        }

        public int NumImportedFuncs()
        {
            int numImportedFuncs = 0;
            foreach (Import import in Imports)
            {
                if (import is FuncImport)
                {
                    numImportedFuncs++;
                }
            }
            return numImportedFuncs;
        }

        public int NumImportedTables()
        {
            int numImportedTables = 0;
            foreach (Import import in Imports)
            {
                if (import is TableImport)
                {
                    numImportedTables++;
                }
            }
            return numImportedTables;
        }

        public int NumImportedMemories()
        {
            int numImportedMemories = 0;
            foreach (Import import in Imports)
            {
                if (import is MemoryImport)
                {
                    numImportedMemories++;
                }
            }
            return numImportedMemories;
        }

        public int NumImportedGlobals()
        {
            int numImportedGlobals = 0;
            foreach (Import import in Imports)
            {
                if (import is GlobalImport)
                {
                    numImportedGlobals++;
                }
            }
            return numImportedGlobals;
        }
    }

    public class Section
    {
        public static readonly Dictionary<byte, Action<BinaryReader, Module>> SectionReaders =
            new Dictionary<byte, Action<BinaryReader, Module>>()
            {
                { 0, ReadCustomSection },
                { 1, ReadTypeSection },
                { 2, ReadImportSection },
                { 3, ReadFunctionSection },
                { 4, ReadTableSection },
                { 5, ReadMemorySection },
                { 6, ReadGlobalSection },
                { 7, ReadExportSection },
                { 8, ReadStartSection },
                { 9, ReadElementSegmentSection },
                { 10, ReadCodeSection },
                { 11, ReadDataSegmentSection },
                { 12, ReadDataCountSection }
            };

        public static string ReadString(BinaryReader stream)
        {
            int len = (int)stream.ReadLEB128Unsigned();
            return Encoding.UTF8.GetString(stream.ReadBytes(len));
        }

        public static void ReadCustomSection(BinaryReader stream, Module module)
        {
            int name_len = (int)stream.ReadLEB128Unsigned();
            string name = Encoding.UTF8.GetString(stream.ReadBytes(name_len));
            int data_len = (int)stream.ReadLEB128Unsigned();
            byte[] data = stream.ReadBytes(data_len);
            module.customData.Add(new CustomData(name, data));
        }

        public static void ReadTypeSection(BinaryReader stream, Module module)
        {
            int num_types = (int)stream.ReadLEB128Unsigned();
            module.FuncTypes = new FuncType[num_types];
            for (int i = 0; i < num_types; i++)
            {
                module.FuncTypes[i] = FuncType.Read(stream);
            }
        }

        public static void ReadImportSection(BinaryReader stream, Module module)
        {
            int numImports = (int)stream.ReadLEB128Unsigned();
            module.Imports = new Import[numImports];
            // Note that in each index space (functions, tables, memories, globals), the indices of
            // the imports go before the first index of any definition contained in the module itself.
            for (int i = 0; i < numImports; i++)
            {
                string module_name = Section.ReadString(stream);
                string name = Section.ReadString(stream);
                byte tag = stream.ReadByte();
                switch (tag)
                {
                    case 0x00:
                        // Since we've already read the FuncTypes, we can resolve the FuncType right away.
                        FuncType funcType = module.FuncTypes[(int)stream.ReadLEB128Unsigned()];
                        module.Funcs.Add(new ImportedFunc(module_name, name, funcType));
                        module.Imports[i] = new FuncImport(module_name, name, funcType);
                        break;

                    case 0x01:
                        TableType tableType = TableType.Read(stream);
                        module.Tables.Add(tableType);
                        module.Imports[i] = new TableImport(module_name, name, tableType);
                        break;

                    case 0x02:
                        Limits memoryType = Limits.Read(stream);
                        module.Memories.Add(memoryType);
                        module.Imports[i] = new MemoryImport(module_name, name, memoryType);
                        break;

                    case 0x03:
                        GlobalType globalType = GlobalType.Read(stream);
                        // Imported globals do not get initialized by modules.
                        GlobalSpec globalSpec = new GlobalSpec(globalType, null);
                        module.Globals.Add(globalSpec);
                        module.Imports[i] = new GlobalImport(module_name, name, globalType);
                        break;

                    default:
                        throw new Trap($"Invalid import tag 0x{tag:2X}");
                }
            }
        }

        public static void ReadFunctionSection(BinaryReader stream, Module module)
        {
            int numFuncs = (int)stream.ReadLEB128Unsigned();
            for (int i = 0; i < numFuncs; i++)
            {
                int funcTypeIdx = (int)stream.ReadLEB128Unsigned();
                module.Funcs.Add(
                    new ModuleFunc(module.ModuleName, $"${i}", module.FuncTypes[funcTypeIdx])
                );
            }
        }

        public static void ReadTableSection(BinaryReader stream, Module module)
        {
            int numTables = (int)stream.ReadLEB128Unsigned();
            for (int i = 0; i < numTables; i++)
            {
                module.Tables.Add(TableType.Read(stream));
            }
        }

        public static void ReadMemorySection(BinaryReader stream, Module module)
        {
            int numMemories = (int)stream.ReadLEB128Unsigned();
            for (int i = 0; i < numMemories; i++)
            {
                module.Memories.Add(Limits.Read(stream));
            }
        }

        public static void ReadGlobalSection(BinaryReader stream, Module module)
        {
            int numGlobals = (int)stream.ReadLEB128Unsigned();
            for (int i = 0; i < numGlobals; i++)
            {
                module.Globals.Add(GlobalSpec.Read(stream));
            }
        }

        public static void ReadExportSection(BinaryReader stream, Module module)
        {
            int numExports = (int)stream.ReadLEB128Unsigned();
            module.Exports = new Export[numExports];
            for (int i = 0; i < numExports; i++)
            {
                string name = ReadString(stream);
                byte tag = stream.ReadByte();
                // The imports have been inserted first in the various lists, so the indices of the exported
                // items will appear to be offset by the number of imports.
                int desc_idx = (int)stream.ReadLEB128Unsigned();
                switch (tag)
                {
                    case 0x00:
                        module.Exports[i] = new FuncExport(name, desc_idx);
                        break;

                    case 0x01:
                        module.Exports[i] = new TableExport(name, desc_idx);
                        break;

                    case 0x02:
                        module.Exports[i] = new MemoryExport(name, desc_idx);
                        break;

                    case 0x03:
                        module.Exports[i] = new GlobalExport(name, desc_idx);
                        break;

                    default:
                        throw new Trap($"Invalid export tag 0x{tag:2X}");
                }
            }
        }

        public static void ReadStartSection(BinaryReader stream, Module module)
        {
            module.StartIdx = (int)stream.ReadLEB128Unsigned();
        }

        public static void ReadElementSegmentSection(BinaryReader stream, Module module)
        {
            int numSegments = (int)stream.ReadLEB128Unsigned();
            module.ElementSegmentSpecs = new ElementSegmentSpec[numSegments];
            for (int i = 0; i < numSegments; i++)
            {
                module.ElementSegmentSpecs[i] = ElementSegmentSpec.Read(stream);
            }
        }

        public static void ReadCodeSection(BinaryReader stream, Module module)
        {
            // Count up the number of imported functions. These functions
            // come after those.
            int numImportedFuncs = module.NumImportedFuncs();

            int numFuncs = (int)stream.ReadLEB128Unsigned();
            for (int i = 0; i < numFuncs; i++)
            {
                int bodySize = (int)stream.ReadLEB128Unsigned(); // not needed
                int numLocalSpecs = (int)stream.ReadLEB128Unsigned();
                List<ValueType> localTypes = new List<ValueType>();
                for (int j = 0; j < numLocalSpecs; j++)
                {
                    int howMany = (int)stream.ReadLEB128Unsigned();
                    ValueType valueType = (ValueType)stream.ReadByte();
                    for (int k = 0; k < howMany; k++)
                    {
                        localTypes.Add(valueType);
                    }
                }
                List<Instruction> body = Module.ReadExpr(stream);
                int funcIdx = numImportedFuncs + i;
                (module.Funcs[funcIdx] as ModuleFunc).Locals = localTypes.ToArray();
                (module.Funcs[funcIdx] as ModuleFunc).Code = body;
            }
        }

        public static void ReadDataSegmentSection(BinaryReader stream, Module module)
        {
            int numSegments = (int)stream.ReadLEB128Unsigned();
            module.DataSegments = new DataSegment[numSegments];
            for (int i = 0; i < numSegments; i++)
            {
                module.DataSegments[i] = DataSegment.Read(stream);
            }
        }

        public static void ReadDataCountSection(BinaryReader stream, Module module)
        {
            module.DataCount = (int)stream.ReadLEB128Unsigned();
        }
    }

    public class CustomData
    {
        public string Name;
        public byte[] Data;

        public CustomData(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }
    }

    public class GlobalSpec
    {
        public GlobalType Type;
        public List<Instruction> InitExpr;

        public GlobalSpec(GlobalType type, List<Instruction> init_expr)
        {
            Type = type;
            InitExpr = init_expr;
        }

        public static GlobalSpec Read(BinaryReader stream)
        {
            GlobalType type = GlobalType.Read(stream);
            List<Instruction> initExpr = Module.ReadExpr(stream);
            return new GlobalSpec(type, initExpr);
        }
    }

    public class Import
    {
        public string ModuleName;
        public string Name;

        public Import(string module_name, string name)
        {
            ModuleName = module_name;
            Name = name;
        }
    }

    public class FuncImport : Import
    {
        public FuncType FuncType;

        public FuncImport(string module_name, string name, FuncType func_type)
            : base(module_name, name)
        {
            FuncType = func_type;
        }

        public override string ToString()
        {
            return $"{ModuleName}.{Name}{FuncType}";
        }
    }

    public class TableImport : Import
    {
        public TableType TableType;

        public TableImport(string module_name, string name, TableType table_type)
            : base(module_name, name)
        {
            TableType = table_type;
        }
    }

    public class MemoryImport : Import
    {
        public Limits MemoryLimits;

        public MemoryImport(string module_name, string name, Limits memory_limits)
            : base(module_name, name)
        {
            MemoryLimits = memory_limits;
        }
    }

    public class GlobalImport : Import
    {
        public GlobalType GlobalType;

        public GlobalImport(string module_name, string name, GlobalType global_type)
            : base(module_name, name)
        {
            GlobalType = global_type;
        }
    }

    public class Export
    {
        public string Name;
        public int Idx;

        public Export(string name, int idx)
        {
            Name = name;
            Idx = idx;
        }
    }

    public class FuncExport : Export
    {
        public FuncExport(string name, int idx)
            : base(name, idx) { }
    }

    public class TableExport : Export
    {
        public TableExport(string name, int idx)
            : base(name, idx) { }
    }

    public class MemoryExport : Export
    {
        public MemoryExport(string name, int idx)
            : base(name, idx) { }
    }

    public class GlobalExport : Export
    {
        public GlobalExport(string name, int idx)
            : base(name, idx) { }
    }

    // An element segment specification.
    //
    // Element segments are used to initialize sections of tables. The elements of tables
    // are always references (either FUNCREF or EXTERNREF).
    //
    // Element segments have a mode that identifies them as either passive, active, or
    // declarative:
    //
    // * A passive element segment's elements can be copied to a table using the table.init
    //   instruction.
    //
    // * An active element segment copies its elements into a table during instantiation,
    //   as specified by a table index and a constant expression defining an offset into
    //   that table.
    //
    // * A declarative element segment is not available at runtime but merely serves to
    //   forward-declare references that are formed in code with instructions like
    //   ref.func.
    public class ElementSegmentSpec
    {
        public ValueType ElemType;

        // ElemIndexes and ElemIdexExprs are mutually exclusive. One will be null.
        public int[] ElemIndexes;
        public List<Instruction>[] ElemIndexExprs;

        public ElementSegmentSpec(ValueType elem_type, int[] elemIndexes)
        {
            ElemType = elem_type;
            ElemIndexes = elemIndexes;
        }

        public ElementSegmentSpec(ValueType elem_type, List<Instruction>[] elemIndexExprs)
        {
            ElemType = elem_type;
            ElemIndexExprs = elemIndexExprs;
        }

        public static ElementSegmentSpec Read(BinaryReader stream)
        {
            byte tag = stream.ReadByte();
            switch (tag)
            {
                case 0x00:
                {
                    // Active segment with default tableidx (0) and default element kind
                    // (FUNCREF).
                    int tableIdx = 0;
                    List<Instruction> offsetExpr = Module.ReadExpr(stream);
                    ValueType elemType = ValueType.FUNCREF;
                    int numIndexes = (int)stream.ReadLEB128Unsigned();
                    int[] elemIndexes = new int[numIndexes];
                    for (int i = 0; i < numIndexes; i++)
                    {
                        elemIndexes[i] = (int)stream.ReadLEB128Unsigned();
                    }
                    return new ActiveElementSegmentSpec(
                        elemType,
                        offsetExpr,
                        tableIdx,
                        elemIndexes
                    );
                }

                case 0x01:
                {
                    int elemType = (int)stream.ReadLEB128Unsigned();
                    if (elemType != 0x00)
                    {
                        throw new Trap($"Invalid element type: 0x{elemType:2X}");
                    }
                    int numIndexes = (int)stream.ReadLEB128Unsigned();
                    int[] elemIndexes = new int[numIndexes];
                    for (int i = 0; i < numIndexes; i++)
                    {
                        elemIndexes[i] = (int)stream.ReadLEB128Unsigned();
                    }
                    return new PassiveElementSegmentSpec(ValueType.FUNCREF, elemIndexes);
                }

                case 0x02:
                {
                    int tableIdx = (int)stream.ReadLEB128Unsigned();
                    List<Instruction> offsetExpr = Module.ReadExpr(stream);
                    int elemType = (int)stream.ReadLEB128Unsigned();
                    if (elemType != 0x00)
                    {
                        throw new Trap($"Invalid element type: 0x{elemType:2X}");
                    }
                    int numIndexes = (int)stream.ReadLEB128Unsigned();
                    int[] elemIndexes = new int[numIndexes];
                    for (int i = 0; i < numIndexes; i++)
                    {
                        elemIndexes[i] = (int)stream.ReadLEB128Unsigned();
                    }
                    return new ActiveElementSegmentSpec(
                        ValueType.FUNCREF,
                        offsetExpr,
                        tableIdx,
                        elemIndexes
                    );
                }

                case 0x03:
                {
                    int elemType = (int)stream.ReadLEB128Unsigned();
                    if (elemType != 0x00)
                    {
                        throw new Trap($"Invalid element type: 0x{elemType:2X}");
                    }
                    int numIndexes = (int)stream.ReadLEB128Unsigned();
                    int[] elemIndexes = new int[numIndexes];
                    for (int i = 0; i < numIndexes; i++)
                    {
                        elemIndexes[i] = (int)stream.ReadLEB128Unsigned();
                    }
                    return new DeclarativeElementSegmentSpec(ValueType.FUNCREF, elemIndexes);
                }

                case 0x04:
                {
                    int tableIdx = 0;
                    List<Instruction> offsetExpr = Module.ReadExpr(stream);
                    ValueType elemType = ValueType.FUNCREF;
                    int numExprs = (int)stream.ReadLEB128Unsigned();
                    List<Instruction>[] elemIndexExprs = new List<Instruction>[numExprs];
                    for (int i = 0; i < numExprs; i++)
                    {
                        elemIndexExprs[i] = Module.ReadExpr(stream);
                    }
                    return new ActiveElementSegmentSpec(
                        ValueType.FUNCREF,
                        offsetExpr,
                        tableIdx,
                        elemIndexExprs
                    );
                }

                case 0x05:
                {
                    ValueType elemType = (ValueType)stream.ReadLEB128Unsigned();
                    int numExprs = (int)stream.ReadLEB128Unsigned();
                    List<Instruction>[] elemIndexExprs = new List<Instruction>[numExprs];
                    for (int i = 0; i < numExprs; i++)
                    {
                        elemIndexExprs[i] = Module.ReadExpr(stream);
                    }
                    return new PassiveElementSegmentSpec(elemType, elemIndexExprs);
                }

                case 0x06:
                {
                    int tableIdx = (int)stream.ReadLEB128Unsigned();
                    List<Instruction> offsetExpr = Module.ReadExpr(stream);
                    ValueType elemType = (ValueType)stream.ReadLEB128Unsigned();
                    int numExprs = (int)stream.ReadLEB128Unsigned();
                    List<Instruction>[] elemIndexExprs = new List<Instruction>[numExprs];
                    for (int i = 0; i < numExprs; i++)
                    {
                        elemIndexExprs[i] = Module.ReadExpr(stream);
                    }
                    return new ActiveElementSegmentSpec(
                        elemType,
                        offsetExpr,
                        tableIdx,
                        elemIndexExprs
                    );
                }

                case 0x07:
                {
                    ValueType elemType = (ValueType)stream.ReadLEB128Unsigned();
                    int numExprs = (int)stream.ReadLEB128Unsigned();
                    List<Instruction>[] elemIndexExprs = new List<Instruction>[numExprs];
                    for (int i = 0; i < numExprs; i++)
                    {
                        elemIndexExprs[i] = Module.ReadExpr(stream);
                    }
                    return new DeclarativeElementSegmentSpec(elemType, elemIndexExprs);
                }

                default:
                    throw new Trap($"Invalid element segment kind: 0x{tag:2X}");
            }
        }
    }

    public class ActiveElementSegmentSpec : ElementSegmentSpec
    {
        public List<Instruction> OffsetExpr;
        public int TableIdx;

        public ActiveElementSegmentSpec(
            ValueType elem_type,
            List<Instruction> offset_expr,
            int tableidx,
            int[] elem_indexes
        )
            : base(elem_type, elem_indexes)
        {
            OffsetExpr = offset_expr;
            TableIdx = tableidx;
        }

        public ActiveElementSegmentSpec(
            ValueType elem_type,
            List<Instruction> offset_expr,
            int tableidx,
            List<Instruction>[] elem_index_exprs
        )
            : base(elem_type, elem_index_exprs)
        {
            OffsetExpr = offset_expr;
            TableIdx = tableidx;
        }
    }

    public class PassiveElementSegmentSpec : ElementSegmentSpec
    {
        public PassiveElementSegmentSpec(ValueType elem_type, int[] elem_indexes)
            : base(elem_type, elem_indexes) { }

        public PassiveElementSegmentSpec(ValueType elem_type, List<Instruction>[] elem_index_exprs)
            : base(elem_type, elem_index_exprs) { }
    }

    public class DeclarativeElementSegmentSpec : ElementSegmentSpec
    {
        public DeclarativeElementSegmentSpec(ValueType elem_type, int[] elem_indexes)
            : base(elem_type, elem_indexes) { }

        public DeclarativeElementSegmentSpec(
            ValueType elem_type,
            List<Instruction>[] elem_index_exprs
        )
            : base(elem_type, elem_index_exprs) { }
    }

    public class DataSegment
    {
        public int MemIdx;
        public byte[] Data;

        public DataSegment(int mem_idx, List<Instruction> offset_expr, byte[] data)
        {
            MemIdx = mem_idx;
            Data = data;
        }

        public static DataSegment Read(BinaryReader stream)
        {
            int tag = (int)stream.ReadLEB128Unsigned();
            switch (tag)
            {
                case 0x00:
                {
                    int memIdx = 0;
                    List<Instruction> offsetExpr = Module.ReadExpr(stream);
                    int size = (int)stream.ReadLEB128Unsigned();
                    byte[] data = stream.ReadBytes(size);
                    return new ActiveDataSegment(memIdx, offsetExpr, data);
                }

                case 0x01:
                {
                    int memIdx = 0;
                    int size = (int)stream.ReadLEB128Unsigned();
                    byte[] data = stream.ReadBytes(size);
                    return new PassiveDataSegment(memIdx, data);
                }

                case 0x02:
                {
                    int memIdx = (int)stream.ReadLEB128Unsigned();
                    List<Instruction> offsetExpr = Module.ReadExpr(stream);
                    int size = (int)stream.ReadLEB128Unsigned();
                    byte[] data = stream.ReadBytes(size);
                    return new ActiveDataSegment(memIdx, offsetExpr, data);
                }

                default:
                    throw new Trap($"Invalid data segment tag: 0x{tag:2X}");
            }
        }
    }

    public class ActiveDataSegment : DataSegment
    {
        public List<Instruction> OffsetExpr;

        public ActiveDataSegment(int mem_idx, List<Instruction> offset_expr, byte[] data)
            : base(mem_idx, offset_expr, data)
        {
            OffsetExpr = offset_expr;
        }
    }

    public class PassiveDataSegment : DataSegment
    {
        public PassiveDataSegment(int mem_idx, byte[] data)
            : base(mem_idx, null, data) { }
    }
}
