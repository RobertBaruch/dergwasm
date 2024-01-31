using System;
using System.Collections.Generic;
using Derg.Modules;
using Derg.Wasm;
using Elements.Core;
using FrooxEngine;

namespace Derg
{
    public class Machine
    {
        bool debug = false;
        public string mainModuleName;

        // mainModuleInstance is used only when EmscriptenEnv constructs an empty frame,
        // since frames need to have a module instance. In that case we use the "main"
        // module instance.
        //
        // The main module instance contains all the mappings of indexes to addresses (e.g.
        // globals, funcs, and so on).
        public ModuleInstance mainModuleInstance;
        public Dictionary<string, HostFunc> hostFuncs = new Dictionary<string, HostFunc>();
        public List<FuncType> funcTypes = new List<FuncType>(); // is this even used?
        public List<Func> funcs = new List<Func>();
        public List<Table> tables = new List<Table>();
        public List<ElementSegment> elementSegments = new List<ElementSegment>();
        public List<Value> Globals = new List<Value>();
        public List<Memory> memories = new List<Memory>();
        public List<byte[]> dataSegments = new List<byte[]>();

        public IWasmAllocator Allocator;

        public unsafe Ptr<T> HeapAlloc<T>(Frame frame)
            where T : struct
        {
            return Allocator.Malloc(frame, sizeof(T)).Reinterpret<T>();
        }

        public unsafe Buff<T> HeapAlloc<T>(Frame frame, int count)
            where T : struct
        {
            return Allocator.Malloc(frame, sizeof(T) * count).Reinterpret<T>().ToBuffer(count);
        }

        public unsafe PrefixBuff<T> HeapAllocPrefix<T>(Frame frame, int count)
            where T : struct
        {
            var prefix = new PrefixBuff<T>(
                Allocator.Malloc(frame, sizeof(int) + sizeof(T) * count)
            );
            HeapSet(prefix.Length, count);
            return prefix;
        }

        // Gets a value from the heap at the given offset plus the given address ("address"
        // in the sense of "address within the memory starting at the offset").
        //
        // Throws a Trap if the offset + address + size of value is out of bounds.
        public unsafe T HeapGet<T>(Ptr<T> addr)
            where T : struct
        {
            Span<byte> mem = HeapSpan(addr);
            fixed (byte* ptr = mem)
            {
                return *(T*)ptr;
            }
        }

        // Gets a value from the heap at the given offset plus the given address ("address"
        // in the sense of "address within the memory starting at the offset").
        //
        // Throws a Trap if the offset + address + size of value is out of bounds.
        public unsafe T HeapGet<T>(int offset, int addr)
            where T : struct
        {
            Span<byte> mem = HeapSpan(offset, addr, sizeof(T));
            fixed (byte* ptr = mem)
            {
                return *(T*)ptr;
            }
        }

        // Sets a value on the heap at the given address.
        //
        // Throws a Trap if the address + value size is out of bounds.
        public unsafe void HeapSet<T>(Ptr<T> addr, T value)
            where T : struct
        {
            Span<byte> mem = HeapSpan(addr, sizeof(T));
            fixed (byte* ptr = mem)
            {
                *(T*)ptr = value;
            }
        }

        // Gets a value on the heap at the given offset plus the given address ("address"
        // in the sense of "address within the memory starting at the offset").
        //
        // Throws a Trap if the offset + address + size of value is out of bounds.
        public unsafe void HeapSet<T>(int offset, int addr, T value)
            where T : struct
        {
            Span<byte> mem = HeapSpan(offset, addr, sizeof(T));
            fixed (byte* ptr = mem)
            {
                *(T*)ptr = value;
            }
        }

        public void HeapSet(Ptr<ulong> ptr, IWorldElement element) =>
            HeapSet(ptr, (ulong)element.ReferenceID);

        public void HeapSet(
            EmscriptenEnv env,
            Frame frame,
            Ptr<NullTerminatedString> ptr,
            string str
        ) => HeapSet(ptr, new NullTerminatedString(env.AllocateUTF8StringInMem(frame, str)));

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
        public unsafe Span<byte> HeapSpan<T>(Ptr<T> offset)
            where T : struct => HeapSpan(offset, sizeof(T));

        // Returns a Span of bytes over the heap, starting from the given offset,
        // with the given size. Note that .NET limits arrays to 2GB, so negative
        // offsets and sizes will lead to an out of bounds condition. Offsets and
        // sizes are ints because that's the way .NET returns array lengths.
        //
        // Throws a Trap if the offset and size are out of bounds.
        public Span<byte> HeapSpan(Ptr offset, int sz)
        {
            try
            {
                return Heap.AsSpan(offset.Addr, sz);
            }
            catch (Exception)
            {
                throw new Trap(
                    $"Memory access out of bounds: offset 0x{(uint)offset.Addr:X8} size 0x{(uint)sz:X8}"
                );
            }
        }

        // Returns a Span of bytes over the heap, starting from the given offset,
        // plus the given address ("address" in the sense of "address within the memory starting
        // at the offset") with the given size. Note that .NET limits arrays to 2GB, so negative
        // offsets and sizes will lead to an out of bounds condition. Offsets and
        // sizes are ints because that's the way .NET returns array lengths.
        //
        // From the spec (https://webassembly.github.io/spec/core/syntax/instructions.html#memory-instructions):
        //
        // "The static address offset is added to the dynamic address operand, yielding a
        // 33 bit effective address that is the zero-based index at which the memory is accessed."
        //
        // This means that the 32-bit offset and address are unsigned, and their addition is NOT
        // modulo 2^32.
        //
        // Throws a Trap if the offset + address + size are out of bounds.
        public Span<byte> HeapSpan(int offset, int address, int sz)
        {
            // Treat as uint, but do ulong math to avoid overflow.
            ulong long_offset = (uint)offset;
            ulong long_address = (uint)address;
            ulong long_size = (uint)sz;
            if (long_offset + long_address + long_size > (ulong)Heap.Length)
            {
                throw new Trap(
                    $"Memory access out of bounds: offset 0x{offset:X8} address 0x{address:X8} size 0x{sz:X8}"
                );
            }
            // Because the heap length cannot be greater than 2GB, we can safely cast everything to int.
            return Heap.AsSpan(offset + address, sz);
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

        public void RegisterReflectedModule<T>(T obj)
        {
            var reflected = new ReflectedModule<T>(obj);
            RegisterModule(reflected);
        }

        public void RegisterModule(IHostModule module)
        {
            foreach (var arg in module.Functions)
            {
                RegisterHostFunc(arg);
            }
        }

        public void RegisterHostFunc(HostFunc hostFunc)
        {
            hostFuncs.Add($"{hostFunc.ModuleName}.{hostFunc.Name}", hostFunc);
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
