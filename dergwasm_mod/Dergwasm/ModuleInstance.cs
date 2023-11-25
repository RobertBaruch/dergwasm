using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    // The runtime representation of a module.
    public class ModuleInstance
    {
        public List<int> FuncTypesMap = new List<int>();
        public List<int> FuncsMap = new List<int>();
        public List<int> TablesMap = new List<int>();
        public List<int> MemoriesMap = new List<int>();
        public List<int> ElementSegmentsMap = new List<int>();
        public List<int> GlobalsMap = new List<int>();
        public List<int> DataSegmentsMap = new List<int>();
        public Dictionary<string, Value> ExportsMap = new Dictionary<string, Value>();

        void AllocateFunctions(IMachine machine, Module module)
        {
            // We only allocate non-imported functions.
            for (int i = module.NumImportedFuncs(); i < module.Funcs.Count; i++)
            {
                ModuleFunc func = module.Funcs[i] as ModuleFunc;
                func.Module = this;
                FuncsMap.Add(machine.AddFunc(func));
            }
        }

        void AllocateTables(IMachine machine, Module module)
        {
            // We only allocate non-imported tables.
            for (int i = module.NumImportedTables(); i < module.Tables.Count; i++)
            {
                TablesMap.Add(machine.AddTable(new Table(module.Tables[i])));
            }
        }

        void AllocateMemories(IMachine machine, Module module)
        {
            // We only allocate non-imported memories.
            for (int i = module.NumImportedMemories(); i < module.Memories.Count; i++)
            {
                MemoriesMap.Add(machine.AddMemory(new Memory(module.Memories[i])));
            }
        }

        void AllocateGlobals(IMachine machine, Module module)
        {
            // We only allocate non-imported globals.
            for (int i = module.NumImportedGlobals(); i < module.Globals.Count; i++)
            {
                GlobalSpec globalSpec = module.Globals[i];
                // The zero value for every type is always represented as all zero!
                GlobalsMap.Add(machine.AddGlobal(new Value()));
            }
        }

        void AllocateElementSegments(IMachine machine, Module module)
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

        void AllocatedDataSegments(IMachine machine, Module module)
        {
            foreach (DataSegment dataSegment in module.DataSegments)
            {
                DataSegmentsMap.Add(machine.AddDataSegment(dataSegment.Data));
            }
        }

        public void Allocate(
            IMachine machine,
            Module module,
            int[] externalFuncAddrs,
            int[] externalTableAddrs,
            int[] externalMemoryAddrs,
            int[] externalGlobalAddrs
        )
        {
            // Imported functions, tables, memories, and globals always come first in the
            // address maps.
            FuncsMap.AddRange(externalFuncAddrs);
            TablesMap.AddRange(externalTableAddrs);
            MemoriesMap.AddRange(externalMemoryAddrs);
            GlobalsMap.AddRange(externalGlobalAddrs);

            AllocateFunctions(machine, module);
            AllocateTables(machine, module);
            AllocateMemories(machine, module);
            AllocateGlobals(machine, module);
            AllocateElementSegments(machine, module);
            AllocatedDataSegments(machine, module);
        }

        void ValidateNumExternsVersusRequiredImports(
            Module module,
            int[] externalFuncAddrs,
            int[] externalTableAddrs,
            int[] externalMemoryAddrs,
            int[] externalGlobalAddrs
        )
        {
            if (externalFuncAddrs.Length != module.NumImportedFuncs())
            {
                List<string> importedFuncs = (
                    from f in module.Imports
                    where f is FuncImport
                    select f.ToString()
                ).ToList();
                throw new Trap(
                    "Wrong number of external function addresses in instantiation of module: "
                        + $"{externalFuncAddrs.Length} provided, but {module.NumImportedFuncs()} were needed:\n"
                        + $"{string.Join("\n", importedFuncs)}"
                );
            }
            if (externalTableAddrs.Length != module.NumImportedTables())
            {
                throw new Trap(
                    "Wrong number of external table addresses in instantiation of module: "
                        + $"{externalTableAddrs.Length} provided, but {module.NumImportedTables()} were needed."
                );
            }
            if (externalMemoryAddrs.Length != module.NumImportedMemories())
            {
                throw new Trap(
                    "Wrong number of external memory addresses in instantiation of module: "
                        + $"{externalMemoryAddrs.Length} provided, but {module.NumImportedMemories()} were needed."
                );
            }
            if (externalGlobalAddrs.Length != module.NumImportedGlobals())
            {
                throw new Trap(
                    "Wrong number of external global addresses in instantiation of module: "
                        + $"{externalGlobalAddrs.Length} provided, but {module.NumImportedGlobals()} were needed."
                );
            }
        }

        void ValidateExternalFuncTypes(IMachine machine, Module module, int[] externalFuncAddrs)
        {
            for (int i = 0; i < externalFuncAddrs.Length; i++)
            {
                Func externalFunc = machine.GetFunc(externalFuncAddrs[i]);
                Func importedFunc = module.Funcs[i];

                if (externalFunc.Signature != importedFunc.Signature)
                {
                    throw new Trap(
                        $"Signature for provided external func for imported func does not match."
                    );
                }
            }
        }

        void ValidateExternalTableTypes(IMachine machine, Module module, int[] externalTableAddrs)
        {
            for (int i = 0; i < externalTableAddrs.Length; i++)
            {
                Table externalTable = machine.GetTable(externalTableAddrs[i]);
                TableType importedTableType = module.Tables[i];

                if (externalTable.Type != importedTableType)
                {
                    throw new Trap(
                        $"Type for provided external table for imported table does not match."
                    );
                }
            }
        }

        void ValidateExternalMemoryTypes(IMachine machine, Module module, int[] externalMemoryAddrs)
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

        void InitGlobals(IMachine machine, Module module)
        {
            // We do not initialize imported globals.
            for (int i = module.NumImportedGlobals(); i < module.Globals.Count; i++)
            {
                GlobalSpec globalSpec = module.Globals[i];
                ModuleFunc syntheticFunc = new ModuleFunc(
                    new FuncType(new ValueType[] { }, new ValueType[] { globalSpec.Type.Type })
                );
                syntheticFunc.Module = this;
                syntheticFunc.Locals = new ValueType[0];
                syntheticFunc.Code = globalSpec.InitExpr;

                machine.Frame = new Frame(syntheticFunc, this);
                machine.PC = -1; // So that incrementing PC goes to beginning.
                machine.Label = new Label(0, syntheticFunc.Code.Count);

                while (machine.HasLabel())
                {
                    machine.Step();
                }

                if (machine.StackLevel() != 1)
                {
                    throw new Trap(
                        "Global init expr did not leave exactly one value on the stack: "
                            + $"{machine.StackLevel()} values are on the stack."
                    );
                }

                machine.Globals[GlobalsMap[i]] = machine.Pop();
                machine.PopFrame();
            }
        }

        public void Instantiate(
            IMachine machine,
            Module module,
            int[] externalFuncAddrs,
            int[] externalTableAddrs,
            int[] externalMemoryAddrs,
            int[] externalGlobalAddrs
        )
        {
            ValidateNumExternsVersusRequiredImports(
                module,
                externalFuncAddrs,
                externalTableAddrs,
                externalMemoryAddrs,
                externalGlobalAddrs
            );

            ValidateExternalFuncTypes(machine, module, externalFuncAddrs);
            ValidateExternalTableTypes(machine, module, externalTableAddrs);
            ValidateExternalMemoryTypes(machine, module, externalMemoryAddrs);
            // We don't keep type information for globals. We probably should.

            Allocate(
                machine,
                module,
                externalFuncAddrs,
                externalTableAddrs,
                externalMemoryAddrs,
                externalGlobalAddrs
            );

            InitGlobals(machine, module);
        }
    }
}
