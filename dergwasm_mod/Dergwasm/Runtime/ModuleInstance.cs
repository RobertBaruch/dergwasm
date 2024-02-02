using System;
using System.Collections.Generic;
using System.Linq;
using Derg.Instructions;

namespace Derg.Runtime
{
    // The runtime representation of a module.
    public class ModuleInstance
    {
        public string ModuleName;
        public List<FuncType> FuncTypes = new List<FuncType>();
        public List<int> FuncsMap = new List<int>();
        public List<int> TablesMap = new List<int>();
        public List<int> MemoriesMap = new List<int>();
        public List<int> ElementSegmentsMap = new List<int>();
        public List<int> GlobalsMap = new List<int>();
        public List<int> DataSegmentsMap = new List<int>();

        public ModuleInstance(string moduleName)
        {
            ModuleName = moduleName;
        }

        void AllocateFunctions(Machine machine, Module module)
        {
            // We only allocate non-imported functions. Imported functions have already
            // been mapped.
            for (int i = module.NumImportedFuncs(); i < module.Funcs.Count; i++)
            {
                ModuleFunc func = module.Funcs[i] as ModuleFunc;
                func.Module = this;
                FuncsMap.Add(machine.AddFunc(func));
            }
        }

        void AllocateTables(Machine machine, Module module)
        {
            // We only allocate non-imported tables. Imported tables have already been
            // mapped.
            for (int i = module.NumImportedTables(); i < module.Tables.Count; i++)
            {
                TablesMap.Add(machine.AddTable(module.Tables[i]));
            }
        }

        void AllocateMemories(Machine machine, Module module)
        {
            // We only allocate non-imported memories. Imported memories have already been
            // mapped.
            for (int i = module.NumImportedMemories(); i < module.Memories.Count; i++)
            {
                MemoriesMap.Add(machine.AddMemory(new Memory(module.Memories[i])));
            }
        }

        void AllocateGlobals(Machine machine, Module module)
        {
            // We only allocate non-imported globals. Imported globals have already been
            // mapped.
            for (int i = module.NumImportedGlobals(); i < module.Globals.Count; i++)
            {
                GlobalSpec globalSpec = module.Globals[i];
                // The zero value for every type is always represented as all zero!
                GlobalsMap.Add(machine.AddGlobal(new Value()));
            }
        }

        void AllocateElementSegments(Machine machine, Module module)
        {
            foreach (ElementSegmentSpec elementSegmentSpec in module.ElementSegmentSpecs)
            {
                Value[] elements =
                    elementSegmentSpec.ElemIndexes != null
                        ? new Value[elementSegmentSpec.ElemIndexes.Length]
                        : new Value[elementSegmentSpec.ElemIndexExprs.Length];
                ElementSegmentsMap.Add(
                    machine.AddElementSegment(
                        new ElementSegment(elementSegmentSpec.ElemType, elements)
                    )
                );
            }
        }

        void AllocatedDataSegments(Machine machine, Module module)
        {
            foreach (DataSegment dataSegment in module.DataSegments)
            {
                DataSegmentsMap.Add(machine.AddDataSegment(dataSegment.Data));
            }
        }

        public void Allocate(Machine machine, Module module)
        {
            // Imported functions, tables, memories, and globals always come first in the
            // address maps.
            FuncsMap.AddRange(module.ExternalFuncAddrs);
            TablesMap.AddRange(module.ExternalTableAddrs);
            MemoriesMap.AddRange(module.ExternalMemoryAddrs);
            GlobalsMap.AddRange(module.ExternalGlobalAddrs);

            AllocateFunctions(machine, module);
            AllocateTables(machine, module);
            AllocateMemories(machine, module);
            AllocateGlobals(machine, module);
            AllocateElementSegments(machine, module);
            AllocatedDataSegments(machine, module);

            FuncTypes.AddRange(module.FuncTypes);
        }

        void ValidateNumExternsVersusRequiredImports(Module module)
        {
            if (module.ExternalFuncAddrs.Length != module.NumImportedFuncs())
            {
                List<string> importedFuncs = (
                    from f in module.Imports
                    where f is FuncImport
                    select f.ToString()
                ).ToList();
                throw new Trap(
                    "Wrong number of external function addresses in instantiation of module: "
                        + $"{module.ExternalFuncAddrs.Length} provided, but {module.NumImportedFuncs()} were needed:\n"
                        + $"{string.Join("\n", importedFuncs)}"
                );
            }
            if (module.ExternalTableAddrs.Length != module.NumImportedTables())
            {
                throw new Trap(
                    "Wrong number of external table addresses in instantiation of module: "
                        + $"{module.ExternalTableAddrs.Length} provided, but {module.NumImportedTables()} were needed."
                );
            }
            if (module.ExternalMemoryAddrs.Length != module.NumImportedMemories())
            {
                throw new Trap(
                    "Wrong number of external memory addresses in instantiation of module: "
                        + $"{module.ExternalMemoryAddrs.Length} provided, but {module.NumImportedMemories()} were needed."
                );
            }
            if (module.ExternalGlobalAddrs.Length != module.NumImportedGlobals())
            {
                throw new Trap(
                    "Wrong number of external global addresses in instantiation of module: "
                        + $"{module.ExternalGlobalAddrs.Length} provided, but {module.NumImportedGlobals()} were needed."
                );
            }
        }

        void ValidateExternalFuncTypes(Machine machine, Module module, int[] externalFuncAddrs)
        {
            for (int i = 0; i < externalFuncAddrs.Length; i++)
            {
                Func externalFunc = machine.GetFunc(externalFuncAddrs[i]);
                Func importedFunc = module.Funcs[i];

                if (externalFunc.Signature != importedFunc.Signature)
                {
                    if (externalFunc is HostFunc hostFunc)
                    {
                        throw new Trap(
                            $"Signature for provided external func {hostFunc.Name} for imported func "
                                + $"does not match: \n"
                                + $"Extern provided: {externalFunc.Signature}\n"
                                + $"Module expected: {importedFunc.Signature}"
                        );
                    }
                    throw new Trap(
                        $"Signature for provided external func for imported func does not match."
                    );
                }
            }
        }

        void ValidateExternalTableTypes(Machine machine, Module module, int[] externalTableAddrs)
        {
            for (int i = 0; i < externalTableAddrs.Length; i++)
            {
                Table externalTable = machine.GetTable(externalTableAddrs[i]);
                TableType importedTableType = module.Tables[i].Type;

                if (externalTable.Type != importedTableType)
                {
                    throw new Trap(
                        $"Type for provided external table for imported table does not match."
                    );
                }
            }
        }

        void ValidateExternalMemoryTypes(Machine machine, Module module, int[] externalMemoryAddrs)
        {
            for (int i = 0; i < externalMemoryAddrs.Length; i++)
            {
                Memory externalMemory = machine.GetMemory(externalMemoryAddrs[i]);
                Limits importedMemoryLimits = module.Memories[i];

                if (externalMemory.Limits != importedMemoryLimits)
                {
                    throw new Trap(
                        $"Limits for provided external memory for imported memory do not match."
                    );
                }
            }
        }

        // Evaluate an expression intended to return a single value. This must only be called
        // during instantiation, when the machine is not running anything.
        Value EvaluateExpr(
            Machine machine,
            Module module,
            string name,
            ValueType returnType,
            List<Instruction> expr
        )
        {
            ModuleFunc syntheticFunc = new ModuleFunc(
                module.ModuleName,
                name,
                new FuncType(new ValueType[] { }, new ValueType[] { returnType })
            );
            syntheticFunc.Module = this;
            syntheticFunc.Locals = new ValueType[0];
            syntheticFunc.Code = expr;

            Frame frame = new Frame(syntheticFunc, this, null);
            frame.Label = new Label(1, expr.Count);
            frame.Execute(machine);
            return frame.Pop();
        }

        void InitGlobals(Machine machine, Module module)
        {
            // We do not initialize imported globals.
            for (int i = module.NumImportedGlobals(); i < module.Globals.Count; i++)
            {
                GlobalSpec globalSpec = module.Globals[i];
                machine.Globals[GlobalsMap[i]] = EvaluateExpr(
                    machine,
                    module,
                    $"<init_global_{GlobalsMap[i]}>",
                    globalSpec.Type.Type,
                    globalSpec.InitExpr
                );

                if (machine.Debug)
                {
                    Console.WriteLine(
                        $"Initialized global {GlobalsMap[i]} to {machine.Globals[GlobalsMap[i]]}"
                    );
                }
            }
        }

        void InitElementSegments(Machine machine, Module module)
        {
            // ElementSegments were added to the instance in the same order as their specs.
            // Thus, ElementSegmentSpec[i] corresponds to ElementSegment[ElementSegmentsMap[i]].
            for (int i = 0; i < module.ElementSegmentSpecs.Length; i++)
            {
                ElementSegmentSpec elementSegmentSpec = module.ElementSegmentSpecs[i];
                ElementSegment elementSegment = machine.GetElementSegment(ElementSegmentsMap[i]);
                // Get element indexes from expressions if necessary.
                if (elementSegmentSpec.ElemIndexes == null)
                {
                    for (int j = 0; j < elementSegmentSpec.ElemIndexExprs.Length; j++)
                    {
                        elementSegment.Elements[j] = EvaluateExpr(
                            machine,
                            module,
                            $"<idx_for_element_seg_{i}_{j}>",
                            elementSegmentSpec.ElemType,
                            elementSegmentSpec.ElemIndexExprs[i]
                        );
                    }
                    continue;
                }
                // Otherwise, we have a list of indexes.
                for (int j = 0; j < elementSegmentSpec.ElemIndexes.Length; j++)
                {
                    int addr = elementSegmentSpec.ElemIndexes[j];
                    // The element type is guaranteed to be a reference type.
                    Value refValue =
                        elementSegmentSpec.ElemType == ValueType.FUNCREF
                            ? Value.RefOfFuncAddr(addr)
                            : Value.RefOfExternAddr(addr);
                    elementSegment.Elements[j] = refValue;
                }
            }
        }

        // Initializes tables from element segments.
        //
        // If an element segment is active, it gets copied into a table during instantiation. But if an
        // element segment is declarative, it is immediately dropped. And if an element segment
        // is passive, nothing happens during instantiation (but tables can be initialized during
        // the running of a module's func).
        void InitTables(Machine machine, Module module)
        {
            for (int i = 0; i < module.ElementSegmentSpecs.Length; i++)
            {
                ElementSegmentSpec elementSegmentSpec = module.ElementSegmentSpecs[i];
                if (elementSegmentSpec is DeclarativeElementSegmentSpec)
                {
                    machine.DropElementSegment(ElementSegmentsMap[i]);
                    continue;
                }
                if (elementSegmentSpec is PassiveElementSegmentSpec)
                {
                    continue;
                }
                ActiveElementSegmentSpec activeElementSegmentSpec =
                    (ActiveElementSegmentSpec)elementSegmentSpec;
                ElementSegment elementSegment = machine.GetElementSegment(ElementSegmentsMap[i]);
                int tableAddr = TablesMap[activeElementSegmentSpec.TableIdx];
                Table table = machine.GetTable(tableAddr);
                int d = EvaluateExpr(
                    machine,
                    module,
                    $"<offset_for_element_seg_{i}_into_table_{tableAddr}>",
                    ValueType.I32,
                    activeElementSegmentSpec.OffsetExpr
                ).s32;
                int n = elementSegment.Elements.Length;
                if (d + n > table.Elements.Length)
                {
                    throw new Trap("table.init during module instantiation: access out of bounds");
                }
                if (n > 0)
                {
                    Array.Copy(elementSegment.Elements, 0, table.Elements, d, n);
                }
                machine.DropElementSegment(ElementSegmentsMap[i]);
            }
        }

        // Initializes memory from data segments.
        //
        // As with tables and element segments, if a data segment is active, it gets copied into
        // memory. Otherwise nothing happens during instantiation (but memory can be initialized
        // during the running of a module's func).
        void InitMemory(Machine machine, Module module)
        {
            for (int i = 0; i < module.DataSegments.Length; i++)
            {
                DataSegment dataSegment = module.DataSegments[i];
                if (dataSegment is PassiveDataSegment)
                {
                    continue;
                }
                ActiveDataSegment activeDataSegment = (ActiveDataSegment)dataSegment;
                int memAddr = MemoriesMap[activeDataSegment.MemIdx];
                Memory memory = machine.GetMemory(memAddr);
                int d = EvaluateExpr(
                    machine,
                    module,
                    $"<offset_for_data_seg_{i}_into_memory_{memAddr}>",
                    ValueType.I32,
                    activeDataSegment.OffsetExpr
                ).s32;
                int n = dataSegment.Data.Length;
                if (d + n > memory.Data.Length)
                {
                    throw new Trap("memory.init during module instantiation: access out of bounds");
                }
                if (n > 0)
                {
                    Array.Copy(dataSegment.Data, 0, memory.Data, d, n);
                }
                machine.DropDataSegment(DataSegmentsMap[i]);
            }
        }

        void MaybeExecuteStartFunc(Machine machine, Module module)
        {
            if (module.StartIdx == -1)
            {
                return;
            }
            int startFuncAddr = FuncsMap[module.StartIdx];
            ModuleFunc startFunc = machine.GetFunc(startFuncAddr) as ModuleFunc;

            Frame frame = new Frame(startFunc, this, null);
            frame.Label = new Label(0, startFunc.Code.Count);
            frame.Execute(machine);
        }

        public void Instantiate(Machine machine, Module module)
        {
            ValidateNumExternsVersusRequiredImports(module);

            ValidateExternalFuncTypes(machine, module, module.ExternalFuncAddrs);
            ValidateExternalTableTypes(machine, module, module.ExternalTableAddrs);
            ValidateExternalMemoryTypes(machine, module, module.ExternalMemoryAddrs);
            // We don't keep type information for globals. We probably should.

            Allocate(machine, module);

            InitGlobals(machine, module);
            InitElementSegments(machine, module);
            InitTables(machine, module);
            InitMemory(machine, module);
            MaybeExecuteStartFunc(machine, module);
        }
    }
}
