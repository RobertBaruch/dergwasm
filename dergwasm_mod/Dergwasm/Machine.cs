using System;
using System.Collections.Generic;
using System.Linq;

namespace Derg
{
    public class Machine
    {
        bool debug = false;
        public string mainModuleName;
        public Dictionary<string, HostFunc> hostFuncs = new Dictionary<string, HostFunc>();
        public List<FuncType> funcTypes = new List<FuncType>(); // is this even used?
        public List<Func> funcs = new List<Func>();
        public List<Table> tables = new List<Table>();
        public List<ElementSegment> elementSegments = new List<ElementSegment>();
        public List<Value> Globals = new List<Value>();
        public List<Memory> memories = new List<Memory>();
        public List<byte[]> dataSegments = new List<byte[]>();

        public string MainModuleName
        {
            get => mainModuleName;
            set => mainModuleName = value;
        }

        public bool Debug
        {
            get => debug;
            set => debug = value;
        }

        public int AddGlobal(Value global)
        {
            Globals.Add(global);
            return Globals.Count - 1;
        }

        public int AddMemory(Memory memory)
        {
            memories.Add(memory);
            return memories.Count - 1;
        }

        public Memory GetMemory(int addr) => memories[addr];

        public Memory GetMemoryFromIndex(int idx)
        {
            if (idx != 0)
            {
                throw new Trap($"Nonzero memory {idx} accessed.");
            }
            return memories[0];
        }

        public byte[] Memory0 => memories[0].Data;

        // Span accepts ints, but converts them internally to uints.
        public Span<byte> Span0(uint offset, uint sz) =>
            new Span<byte>(memories[0].Data, (int)offset, (int)sz);

        public int AddFunc(Func func)
        {
            funcs.Add(func);
            return funcs.Count - 1;
        }

        public int NumFuncs => funcs.Count;

        public Func GetFunc(int addr) => funcs[addr];

        public Func GetFunc(string moduleName, string name)
        {
            // O(N) for now
            foreach (var f in funcs)
            {
                if (f.ModuleName == moduleName && f.Name == name)
                {
                    return f;
                }
            }
            return null;
        }

        public int AddTable(Table table)
        {
            tables.Add(table);
            return tables.Count - 1;
        }

        public Table GetTable(int addr) => tables[addr];

        public Table GetTable(string moduleName, string name)
        {
            // O(N) for now
            foreach (var t in tables)
            {
                if (t.ModuleName == moduleName && t.Name == name)
                {
                    return t;
                }
            }
            return null;
        }

        public int AddElementSegment(ElementSegment elementSegment)
        {
            elementSegments.Add(elementSegment);
            return elementSegments.Count - 1;
        }

        public void DropElementSegment(int addr) => elementSegments[addr] = null;

        public ElementSegment GetElementSegment(int addr) => elementSegments[addr];

        public int AddDataSegment(byte[] dataSegment)
        {
            dataSegments.Add(dataSegment);
            return dataSegments.Count - 1;
        }

        public void DropDataSegment(int addr) => dataSegments[addr] = null;

        public byte[] GetDataSegment(int addr) => dataSegments[addr];

        public void RegisterHostFunc(
            string moduleName,
            string name,
            FuncType signature,
            HostProxy proxy
        )
        {
            hostFuncs.Add($"{moduleName}.{name}", new HostFunc(moduleName, name, signature, proxy));
        }

        public int ResolveHostFunc(string moduleName, string name, FuncType signature)
        {
            string key = $"{moduleName}.{name}";
            if (!hostFuncs.ContainsKey(key))
            {
                throw new Trap($"Could not find host function {key} {signature}");
            }
            funcs.Add(hostFuncs[key]);
            return funcs.Count - 1;
        }
    }
}
