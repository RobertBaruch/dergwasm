﻿using Elements.Assets;
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
            return SlotFromRefID(((ulong)slot_id_hi << 32) | slot_id_lo);
        }

        public Slot SlotFromRefID(ulong slot_id)
        {
            RefID refID = new RefID(slot_id);
            return world.ReferenceController.GetObjectOrNull(refID) as Slot;
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
                        int ptr = emscriptenEnv.AllocateUTF8StringInMem(null, stringField.Value);
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
            Frame frame = emscriptenEnv.EmptyFrame(f as ModuleFunc);
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
                    emscriptenEnv.free(emscriptenEnv.EmptyFrame(), ptr);
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
            machine.RegisterReturningHostFunc<ulong>("env", "slot__root_slot", slot__root_slot);
            machine.RegisterReturningHostFunc<ulong, ulong>(
                "env",
                "slot__get_parent",
                slot__get_parent
            );
            machine.RegisterReturningHostFunc<ulong, ulong>(
                "env",
                "slot__get_active_user",
                slot__get_active_user
            );
            machine.RegisterReturningHostFunc<ulong, ulong>(
                "env",
                "slot__get_active_user_root",
                slot__get_active_user_root
            );
            machine.RegisterReturningHostFunc<ulong, int, ulong>(
                "env",
                "slot__get_object_root",
                slot__get_object_root
            );
            machine.RegisterReturningHostFunc<ulong, int>("env", "slot__get_name", slot__get_name);
            machine.RegisterVoidHostFunc<ulong, int>("env", "slot__set_name", slot__set_name);
            machine.RegisterReturningHostFunc<ulong, int>(
                "env",
                "value_field__bool__get_value",
                value_field__bool__get_value
            );
            machine.RegisterVoidHostFunc<ulong, int>(
                "env",
                "value_field__bool__set_value",
                value_field__bool__set_value
            );
        }

        //
        // The host functions. They are always called from WASM, so they already have a frame.
        //

        public ulong slot__root_slot(Frame frame)
        {
            Slot slot = world.RootSlot;
            return (ulong)slot.ReferenceID;
        }

        public ulong slot__get_parent(Frame frame, ulong slot_id)
        {
            Slot slot = SlotFromRefID(slot_id);
            return ((ulong?)slot?.Parent?.ReferenceID) ?? 0;
        }

        public ulong slot__get_active_user(Frame frame, ulong slot_id)
        {
            Slot slot = SlotFromRefID(slot_id);
            return ((ulong?)slot?.ActiveUser?.ReferenceID) ?? 0;
        }

        public ulong slot__get_active_user_root(Frame frame, ulong slot_id)
        {
            Slot slot = SlotFromRefID(slot_id);
            return ((ulong?)slot?.ActiveUserRoot?.ReferenceID) ?? 0;
        }

        public ulong slot__get_object_root(Frame frame, ulong slot_id, int only_explicit)
        {
            Slot slot = SlotFromRefID(slot_id);
            return ((ulong?)slot?.GetObjectRoot(only_explicit != 0)?.ReferenceID) ?? 0;
        }

        public int slot__get_name(Frame frame, ulong slot_id)
        {
            Slot slot = SlotFromRefID(slot_id);
            string name = slot?.Name ?? "";
            return emscriptenEnv.AllocateUTF8StringInMem(frame, name);
        }

        public void slot__set_name(Frame frame, ulong slot_id, int ptr)
        {
            Slot slot = SlotFromRefID(slot_id);
            if (slot == null)
                return;
            slot.Name = emscriptenEnv.GetUTF8StringFromMem(ptr);
        }

        public void value_field__bool__set_value(Frame frame, ulong value_field_id, int value)
        {
            ValueField<bool> component =
                world.ReferenceController.GetObjectOrNull(new RefID(value_field_id))
                as ValueField<bool>;
            if (component == null)
                return;
            component.Value.Value = value != 0;
        }

        public int value_field__bool__get_value(Frame frame, ulong value_field_id)
        {
            ValueField<bool> component =
                world.ReferenceController.GetObjectOrNull(new RefID(value_field_id))
                as ValueField<bool>;
            return (component?.Value?.Value ?? false) ? 1 : 0;
        }
    }
}
