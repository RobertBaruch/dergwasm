using System;
using System.Collections.Generic;

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

        public unsafe T HeapGet<T>(uint ea)
            where T : unmanaged
        {
            try
            {
                fixed (byte* ptr = &Heap[ea])
                {
                    return *(T*)ptr;
                }
            }
            catch (Exception)
            {
                throw new Trap(
                    $"Memory access out of bounds: reading {sizeof(T)} bytes at 0x{ea:X8}"
                );
            }
        }

        public unsafe T HeapGet<T>(int ea)
            where T : unmanaged
        {
            return HeapGet<T>((uint)ea);
        }

        public unsafe void HeapSet<T>(uint ea, T value)
            where T : unmanaged
        {
            try
            {
                Span<byte> mem = HeapSpan((int)ea, sizeof(T));
                fixed (byte* ptr = mem)
                {
                    *(T*)ptr = value;
                }
            }
            catch (Exception)
            {
                throw new Trap(
                    $"Memory access out of bounds: writing {sizeof(T)} bytes at 0x{ea:X8}"
                );
            }
        }

        public unsafe void HeapSet<T>(int ea, T value)
            where T : unmanaged
        {
            HeapSet((uint)ea, value);
        }

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

        // Returns the backing byte array of the first memory (i.e. the heap).
        public byte[] Heap => memories[0].Data;

        // Returns a Span of bytes over the heap, starting from the given offset,
        // with the given size. Note that .NET limits arrays to 2GB, so negative
        // offsets and sizes will lead to an out of bounds condition. Offsets and
        // sizes are ints because that's the way .NET returns array lengths.
        //
        // Throws a Trap if the offset and size are out of bounds.
        public Span<byte> HeapSpan(int offset, int sz)
        {
            try
            {
                return Heap.AsSpan(offset, sz);
            }
            catch (Exception)
            {
                throw new Trap(
                    $"Memory access out of bounds: offset 0x{(uint)offset:X8} size 0x{(uint)sz:X8}"
                );
            }
        }

        public int AddFunc(Func func)
        {
            funcs.Add(func);
            return funcs.Count - 1;
        }

        public int NumFuncs => funcs.Count;

        public Func GetFunc(int addr) => funcs[addr];

        public Func GetFunc(string moduleName, string name, bool throw_if_not_found = false)
        {
            // O(N) for now
            foreach (var f in funcs)
            {
                if (f.ModuleName == moduleName && f.Name == name)
                {
                    return f;
                }
            }
            if (throw_if_not_found)
            {
                throw new Trap($"Could not find function {moduleName}.{name} in WASM");
            }
            return null;
        }

        public Func GetRequiredFunc(string moduleName, string name) =>
            GetFunc(
                moduleName,
                name, /*throw_if_not_found=*/
                true
            );

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

        public void RegisterVoidHostFunc(string moduleName, string name, Action<Frame> func)
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(new ValueType[] { }, new ValueType[] { }),
                new VoidHostProxy(func)
            );
        }

        public void RegisterVoidHostFunc<T1>(string moduleName, string name, Action<Frame, T1> func)
            where T1 : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(new ValueType[] { Value.ValueType<T1>() }, new ValueType[] { }),
                new HostProxy<T1>(func)
            );
        }

        public void RegisterVoidHostFunc<T1, T2>(
            string moduleName,
            string name,
            Action<Frame, T1, T2> func
        )
            where T1 : unmanaged
            where T2 : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(
                    new ValueType[] { Value.ValueType<T1>(), Value.ValueType<T2>() },
                    new ValueType[] { }
                ),
                new HostProxy<T1, T2>(func)
            );
        }

        public void RegisterVoidHostFunc<T1, T2, T3>(
            string moduleName,
            string name,
            Action<Frame, T1, T2, T3> func
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(
                    new ValueType[]
                    {
                        Value.ValueType<T1>(),
                        Value.ValueType<T2>(),
                        Value.ValueType<T3>()
                    },
                    new ValueType[] { }
                ),
                new HostProxy<T1, T2, T3>(func)
            );
        }

        public void RegisterVoidHostFunc<T1, T2, T3, T4>(
            string moduleName,
            string name,
            Action<Frame, T1, T2, T3, T4> func
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(
                    new ValueType[]
                    {
                        Value.ValueType<T1>(),
                        Value.ValueType<T2>(),
                        Value.ValueType<T3>(),
                        Value.ValueType<T4>()
                    },
                    new ValueType[] { }
                ),
                new HostProxy<T1, T2, T3, T4>(func)
            );
        }

        public void RegisterVoidHostFunc<T1, T2, T3, T4, T5>(
            string moduleName,
            string name,
            Action<Frame, T1, T2, T3, T4, T5> func
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(
                    new ValueType[]
                    {
                        Value.ValueType<T1>(),
                        Value.ValueType<T2>(),
                        Value.ValueType<T3>(),
                        Value.ValueType<T4>(),
                        Value.ValueType<T5>()
                    },
                    new ValueType[] { }
                ),
                new HostProxy<T1, T2, T3, T4, T5>(func)
            );
        }

        public void RegisterReturningHostFunc<R>(
            string moduleName,
            string name,
            Func<Frame, R> func
        )
            where R : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(new ValueType[] { }, new ValueType[] { Value.ValueType<R>() }),
                new ReturningVoidHostProxy<R>(func)
            );
        }

        public void RegisterReturningHostFunc<T1, R>(
            string moduleName,
            string name,
            Func<Frame, T1, R> func
        )
            where T1 : unmanaged
            where R : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(
                    new ValueType[] { Value.ValueType<T1>() },
                    new ValueType[] { Value.ValueType<R>() }
                ),
                new ReturningHostProxy<T1, R>(func)
            );
        }

        public void RegisterReturningHostFunc<T1, T2, R>(
            string moduleName,
            string name,
            Func<Frame, T1, T2, R> func
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where R : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(
                    new ValueType[] { Value.ValueType<T1>(), Value.ValueType<T2>() },
                    new ValueType[] { Value.ValueType<R>() }
                ),
                new ReturningHostProxy<T1, T2, R>(func)
            );
        }

        public void RegisterReturningHostFunc<T1, T2, T3, R>(
            string moduleName,
            string name,
            Func<Frame, T1, T2, T3, R> func
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where R : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(
                    new ValueType[]
                    {
                        Value.ValueType<T1>(),
                        Value.ValueType<T2>(),
                        Value.ValueType<T3>()
                    },
                    new ValueType[] { Value.ValueType<R>() }
                ),
                new ReturningHostProxy<T1, T2, T3, R>(func)
            );
        }

        public void RegisterReturningHostFunc<T1, T2, T3, T4, R>(
            string moduleName,
            string name,
            Func<Frame, T1, T2, T3, T4, R> func
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where R : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(
                    new ValueType[]
                    {
                        Value.ValueType<T1>(),
                        Value.ValueType<T2>(),
                        Value.ValueType<T3>(),
                        Value.ValueType<T4>()
                    },
                    new ValueType[] { Value.ValueType<R>() }
                ),
                new ReturningHostProxy<T1, T2, T3, T4, R>(func)
            );
        }

        public void RegisterReturningHostFunc<T1, T2, T3, T4, T5, R>(
            string moduleName,
            string name,
            Func<Frame, T1, T2, T3, T4, T5, R> func
        )
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where R : unmanaged
        {
            RegisterHostFunc(
                moduleName,
                name,
                new FuncType(
                    new ValueType[]
                    {
                        Value.ValueType<T1>(),
                        Value.ValueType<T2>(),
                        Value.ValueType<T3>(),
                        Value.ValueType<T4>(),
                        Value.ValueType<T5>()
                    },
                    new ValueType[] { Value.ValueType<R>() }
                ),
                new ReturningHostProxy<T1, T2, T3, T4, T5, R>(func)
            );
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
