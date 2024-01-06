using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Derg
{
    // Provides the functions in resonite_api.h. A ResoniteEnv, like a Machine, is specific
    // to a World.
    //
    // In the API, we don't use anything other than ints, floats, and doubles. We can't use
    // longs because Emscripten doesn't support them yet. Pointers to memory are uints.
    public class ResoniteEnv
    {
        public Machine machine;
        public World world;
        public EmscriptenEnv emscriptenEnv;

        // Could these be persistent across calls? If we could patch into Resonite so that
        // we get told if any of these are disposed, then we could remove them from the
        // dictionaries.
        //
        // This would enable us to have collections of slots and users.
        public Dictionary<RefID, Slot> slotDict;
        public Dictionary<RefID, User> userDict;

        public ResoniteEnv(Machine machine, World world, EmscriptenEnv emscriptenEnv)
        {
            this.machine = machine;
            this.world = world;
            this.emscriptenEnv = emscriptenEnv;
            slotDict = new Dictionary<RefID, Slot>();
            userDict = new Dictionary<RefID, User>();
        }

        private unsafe T MemGet<T>(uint ea)
            where T : unmanaged
        {
            fixed (byte* ptr = &machine.Memory0[ea])
            {
                return *(T*)ptr;
            }
        }

        private unsafe void MemSet<T>(uint ea, T value)
            where T : unmanaged
        {
            try
            {
                Span<byte> mem = machine.Span0(ea, (uint)sizeof(T));
                fixed (byte* ptr = mem)
                {
                    *(T*)ptr = value;
                }
            }
            catch (Exception e)
            {
                throw new Trap($"Memory access out of bounds: {sizeof(T)} bytes at 0x{ea:X8}");
            }
        }

        public Slot SlotFromRefID(uint slot_id_lo, uint slot_id_hi)
        {
            RefID refID = new RefID(((ulong)slot_id_hi << 32) | slot_id_lo);
            return world.ReferenceController.GetObjectOrNull(refID) as Slot;
        }

        // Calls a WASM function, where the function to call and its arguments are stored in
        // the given argsSlot.
        public void CallWasmFunction(Slot argsSlot)
        {
            Frame frame = new Frame(null, null, null);
            frame.Label = new Label(1, 0);
            string funcName = null;
            List<Value> args = new List<Value>();

            // The list of stuff we have to free after the call.
            // TODO: What happens if WASM traps? Is the machine even valid anymore?
            List<int> allocations = new List<int>();

            // The ValueField components in the argsSlot are the function name and its arguments.
            // TODO: Should this be Children or LocalChildren? Children calls EnsureChildOrder, which
            // seems like a good thing.
            foreach (Slot child in argsSlot.Children)
            {
                foreach (Component c in child.Components)
                {
                    if (c is ValueField<string> stringField)
                    {
                        if (funcName == null)
                        {
                            funcName = stringField.Value;
                            break;
                        }
                        byte[] stringData = Encoding.UTF8.GetBytes(stringField.Value);
                        int stringPtr = emscriptenEnv.malloc(frame, stringData.Length + 1);
                        allocations.Add(stringPtr);
                        Array.Copy(stringData, 0, machine.Memory0, stringPtr, stringData.Length);
                        machine.Memory0[stringPtr + stringData.Length] = 0; // NUL-termination
                        break;
                    }

                    if (c is ValueField<Slot> slotField)
                    {
                        slotDict.Add(slotField.Value.ReferenceID, slotField.Value);
                        args.Add(new Value((ulong)slotField.Value.ReferenceID));
                        break;
                    }

                    if (c is ValueField<User> userField)
                    {
                        userDict.Add(userField.Value.ReferenceID, userField.Value);
                        args.Add(new Value((ulong)userField.Value.ReferenceID));
                        break;
                    }

                    if (c is ValueField<bool> boolField)
                    {
                        args.Add(new Value(boolField.Value));
                        break;
                    }
                    // TODO: The rest of the types
                }
            }

            // TODO: Execute the exported function.

            slotDict.Clear();
            userDict.Clear();
            foreach (int ptr in allocations)
            {
                emscriptenEnv.free(frame, ptr);
            }
        }

        //
        // Host function registration.
        //

        // This only needs to be called once, when the WASM Machine is initialized and loaded.
        public void RegisterHostFuncs()
        {
            machine.RegisterVoidHostFunc<uint>("env", "slot__root_slot", slot__root_slot);
            machine.RegisterVoidHostFunc<uint, uint, uint>(
                "env",
                "slot__get_parent",
                slot__get_parent
            );
            machine.RegisterVoidHostFunc<uint, uint, uint>(
                "env",
                "slot__get_active_user",
                slot__get_active_user
            );
            machine.RegisterVoidHostFunc<uint, uint, uint>(
                "env",
                "slot__get_active_user_root",
                slot__get_active_user_root
            );
            machine.RegisterVoidHostFunc<uint, uint, int, uint>(
                "env",
                "slot__get_object_root",
                slot__get_object_root
            );
            machine.RegisterReturningHostFunc<uint, uint, int>(
                "env",
                "slot__get_name",
                slot__get_name
            );
        }

        //
        // The host functions.
        //

        public void slot__root_slot(Frame frame, uint rootPtr)
        {
            Slot slot = world.RootSlot;
            MemSet(rootPtr, (ulong)slot.ReferenceID);
        }

        public void slot__get_parent(Frame frame, uint slot_id_lo, uint slot_id_hi, uint parentPtr)
        {
            Slot slot = SlotFromRefID(slot_id_lo, slot_id_hi);
            MemSet(parentPtr, ((ulong?)slot?.Parent?.ReferenceID) ?? 0);
        }

        public void slot__get_active_user(
            Frame frame,
            uint slot_id_lo,
            uint slot_id_hi,
            uint userPtr
        )
        {
            Slot slot = SlotFromRefID(slot_id_lo, slot_id_hi);
            MemSet(userPtr, ((ulong?)slot?.ActiveUser?.ReferenceID) ?? 0);
        }

        public void slot__get_active_user_root(
            Frame frame,
            uint slot_id_lo,
            uint slot_id_hi,
            uint userRootPtr
        )
        {
            Slot slot = SlotFromRefID(slot_id_lo, slot_id_hi);
            MemSet(userRootPtr, ((ulong?)slot?.ActiveUserRoot?.ReferenceID) ?? 0);
        }

        public void slot__get_object_root(
            Frame frame,
            uint slot_id_lo,
            uint slot_id_hi,
            int only_explicit,
            uint objectRootPtr
        )
        {
            Slot slot = SlotFromRefID(slot_id_lo, slot_id_hi);
            MemSet(
                objectRootPtr,
                ((ulong?)slot?.GetObjectRoot(only_explicit != 0)?.ReferenceID) ?? 0
            );
        }

        public int slot__get_name(Frame frame, uint slot_id_lo, uint slot_id_hi)
        {
            Slot slot = SlotFromRefID(slot_id_lo, slot_id_hi);
            string name = slot?.Name ?? "";
            byte[] nameData = Encoding.UTF8.GetBytes(name);
            int namePtr = emscriptenEnv.malloc(frame, nameData.Length + 1);
            Array.Copy(nameData, 0, machine.Memory0, namePtr, nameData.Length);
            machine.Memory0[namePtr + nameData.Length] = 0; // NUL-termination
            return namePtr;
        }
    }
}
