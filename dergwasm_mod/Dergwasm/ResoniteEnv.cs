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

        // This only needs to be called once, when the WASM Machine is initialized and loaded.
        public void RegisterHostFuncs()
        {
            machine.RegisterReturningHostFunc<ulong, ulong>(
                "resonite_api", // TODO: Is this right?
                "slot__get_active_user",
                slot__get_active_user
            );
            machine.RegisterReturningHostFunc<ulong, ulong>(
                "resonite_api",
                "slot__get_active_user_root",
                slot__get_active_user_root
            );
            machine.RegisterReturningHostFunc<ulong, int, ulong>(
                "resonite_api",
                "slot__get_object_root",
                slot__get_object_root
            );
            machine.RegisterReturningHostFunc<ulong, ulong>(
                "resonite_api",
                "slot__get_parent",
                slot__get_parent
            );
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

        public ulong slot__get_active_user(Frame frame, ulong slot_id)
        {
            Slot slot = null;
            if (!slotDict.TryGetValue(slot_id, out slot))
            {
                return 0;
            }
            return (ulong)slot.ActiveUser.ReferenceID;
        }

        public ulong slot__get_active_user_root(Frame frame, ulong slot_id)
        {
            Slot slot = null;
            if (!slotDict.TryGetValue(slot_id, out slot))
            {
                return 0;
            }
            return (ulong)slot.ActiveUserRoot.ReferenceID;
        }

        public ulong slot__get_object_root(Frame frame, ulong slot_id, int only_explicit)
        {
            Slot slot = null;
            if (!slotDict.TryGetValue(slot_id, out slot))
            {
                return 0;
            }
            Slot root = slot.GetObjectRoot(only_explicit != 0);
            if (root == null)
            {
                return 0;
            }
            slotDict.Add(root.ReferenceID, root);
            return (ulong)root.ReferenceID;
        }

        public ulong slot__get_parent(Frame frame, ulong slot_id)
        {
            Slot slot = null;
            if (!slotDict.TryGetValue(slot_id, out slot))
            {
                return 0;
            }
            Slot parent = slot.Parent;
            if (parent == null)
            {
                return 0;
            }
            slotDict.Add(parent.ReferenceID, parent);
            return (ulong)parent.ReferenceID;
        }
    }
}
