using Elements.Assets;
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

        public ResoniteEnv(Machine machine, World world, EmscriptenEnv emscriptenEnv)
        {
            this.machine = machine;
            this.world = world;
            this.emscriptenEnv = emscriptenEnv;
        }

        public Slot SlotFromRefID(uint slot_id_lo, uint slot_id_hi)
        {
            RefID refID = new RefID(((ulong)slot_id_hi << 32) | slot_id_lo);
            return world.ReferenceController.GetObjectOrNull(refID) as Slot;
        }

        // Creates an empty frame which can be used to call a WASM function, if you weren't
        // already in a frame. Specify the ModuleFunc if you are going to use this to call
        // a WASM function.
        public Frame EmptyFrame(ModuleFunc f = null)
        {
            Frame frame = new Frame(f, DergwasmMachine.moduleInstance, null);
            frame.Label = new Label(0, 0);
            return frame;
        }

        public int allocateString(Frame frame, string s)
        {
            if (frame == null)
                frame = EmptyFrame();

            byte[] stringData = Encoding.UTF8.GetBytes(s);
            int stringPtr = emscriptenEnv.malloc(frame, stringData.Length + 1);
            Array.Copy(stringData, 0, machine.Memory0, stringPtr, stringData.Length);
            machine.Memory0[stringPtr + stringData.Length] = 0; // NUL-termination
            return stringPtr;
        }

        List<Value> ExtractArgs(Slot argsSlot, List<int> allocations)
        {
            List<Value> args = new List<Value>();

            // The ValueField components in the argsSlot children are the arguments.
            // Children calls EnsureChildOrder, which is a good thing.
            foreach (Slot child in argsSlot.Children)
            {
                // We only look at the first ValueField we find. Adding more than one ValueField
                // in a slot leads to undefined behavior.
                foreach (Component c in child.Components)
                {
                    if (c is ValueField<string> stringField)
                    {
                        int ptr = allocateString(null, stringField.Value);
                        allocations.Add(ptr);
                        args.Add(new Value(ptr));
                        DergwasmMachine.Msg($"String arg, ptr = 0x{ptr:X8}");
                        break;
                    }

                    if (c is ValueField<Slot> slotField)
                    {
                        args.Add(new Value((uint)slotField.Value.ReferenceID));
                        args.Add(new Value((uint)(slotField.Value.ReferenceID >> 32)));
                        DergwasmMachine.Msg($"Slot arg: ID {slotField.Value.ReferenceID}");
                        break;
                    }

                    if (c is ValueField<User> userField)
                    {
                        args.Add(new Value((uint)userField.Value.ReferenceID));
                        args.Add(new Value((uint)(userField.Value.ReferenceID >> 32)));
                        DergwasmMachine.Msg($"User arg: ID {userField.Value.ReferenceID}");
                        break;
                    }

                    if (c is ValueField<bool> boolField)
                    {
                        args.Add(new Value(boolField.Value));
                        DergwasmMachine.Msg($"Bool arg: {boolField.Value}");
                        break;
                    }
                    // TODO: The rest of the types
                }
            }

            return args;
        }

        string ExtractFuncName(Slot argsSlot)
        {
            // We only look at the first ValueField we find. Adding more than one ValueField
            // in a slot leads to undefined behavior.
            foreach (Component c in argsSlot.Components)
            {
                if (c is ValueField<string> stringField)
                {
                    return stringField.Value;
                }
            }
            return null;
        }

        // Invokes a WASM function when you're not already in a WASM function.
        void InvokeWasmFunction(string funcName, List<Value> args)
        {
            Func f = machine.GetFunc(DergwasmMachine.moduleInstance.ModuleName, funcName);
            if (f == null)
            {
                throw new Trap($"No {funcName} function found");
            }
            DergwasmMachine.Msg($"Running {funcName}");
            Frame frame = EmptyFrame(f as ModuleFunc);
            foreach (Value arg in args)
            {
                frame.Push(arg);
            }
            frame.InvokeFunc(machine, f);
        }

        // Calls a WASM function, where the function to call and its arguments are stored in
        // the given argsSlot. The name of the function to call is in a ValueField<string> on
        // the argsSlot, while the arguments are in ValueFields on the argsSlot's children.
        public void CallWasmFunction(Slot argsSlot)
        {
            string funcName = ExtractFuncName(argsSlot);
            if (funcName == null)
            {
                DergwasmMachine.Msg("No ValueField<string> component found on args slot");
                return;
            }
            List<int> argAllocations = new List<int>();
            List<Value> args = ExtractArgs(argsSlot, argAllocations);

            try
            {
                InvokeWasmFunction(funcName, args);
            }
            catch (ExitTrap)
            {
                DergwasmMachine.Msg($"{funcName} exited");
            }
            finally
            {
                foreach (int ptr in argAllocations)
                {
                    emscriptenEnv.free(EmptyFrame(), ptr);
                }
                DergwasmMachine.Msg("Call complete");
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
        // The host functions. They are always called from WASM, so they already have a frame.
        //

        public void slot__root_slot(Frame frame, uint rootPtr)
        {
            Slot slot = world.RootSlot;
            machine.MemSet(rootPtr, (ulong)slot.ReferenceID);
        }

        public void slot__get_parent(Frame frame, uint slot_id_lo, uint slot_id_hi, uint parentPtr)
        {
            Slot slot = SlotFromRefID(slot_id_lo, slot_id_hi);
            machine.MemSet(parentPtr, ((ulong?)slot?.Parent?.ReferenceID) ?? 0);
        }

        public void slot__get_active_user(
            Frame frame,
            uint slot_id_lo,
            uint slot_id_hi,
            uint userPtr
        )
        {
            Slot slot = SlotFromRefID(slot_id_lo, slot_id_hi);
            machine.MemSet(userPtr, ((ulong?)slot?.ActiveUser?.ReferenceID) ?? 0);
        }

        public void slot__get_active_user_root(
            Frame frame,
            uint slot_id_lo,
            uint slot_id_hi,
            uint userRootPtr
        )
        {
            Slot slot = SlotFromRefID(slot_id_lo, slot_id_hi);
            machine.MemSet(userRootPtr, ((ulong?)slot?.ActiveUserRoot?.ReferenceID) ?? 0);
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
            machine.MemSet(
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

        public int slot__set_name(Frame frame, uint slot_id_lo, uint slot_id_hi, uint ptr)
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
