using System;
using System.Collections.Generic;
using Elements.Core;
using FrooxEngine;

namespace Derg
{
    // Provides the functions in resonite_api.h. A ResoniteEnv, like a Machine, is specific
    // to a World.
    //
    // In the API, we don't use anything other than ints, longs, floats, and doubles.
    // Pointers to memory are uints.
    public class ResoniteEnv
    {
        public Machine machine;
        public IWorldServices worldServices;
        public EmscriptenEnv emscriptenEnv;

        public ResoniteEnv(
            Machine machine,
            IWorldServices worldServices,
            EmscriptenEnv emscriptenEnv
        )
        {
            this.machine = machine;
            this.worldServices = worldServices;
            this.emscriptenEnv = emscriptenEnv;
        }

        public IWorldElement FromRefID(uint slot_id_lo, uint slot_id_hi)
        {
            return FromRefID(((ulong)slot_id_hi << 32) | slot_id_lo);
        }

        public T FromRefID<T>(uint slot_id_lo, uint slot_id_hi)
            where T : class, IWorldElement
        {
            return FromRefID(slot_id_lo, slot_id_hi) as T;
        }

        public T FromRefID<T>(ulong slot_id)
            where T : class, IWorldElement
        {
            RefID refID = new RefID(slot_id);
            return worldServices.GetObjectOrNull(refID) as T;
        }

        public IWorldElement FromRefID(ulong slot_id)
        {
            RefID refID = new RefID(slot_id);
            return worldServices.GetObjectOrNull(refID);
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
                        args.Add(new Value { s32 = ptr });
                        DergwasmMachine.Msg($"String arg, ptr = 0x{ptr:X8}");
                        break;
                    }

                    if (c is ValueField<Slot> slotField)
                    {
                        args.Add(new Value { u64 = (ulong)slotField.Value.ReferenceID });
                        DergwasmMachine.Msg($"Slot arg: ID {slotField.Value.ReferenceID}");
                        break;
                    }

                    if (c is ValueField<User> userField)
                    {
                        args.Add(new Value { u64 = (ulong)userField.Value.ReferenceID });
                        DergwasmMachine.Msg($"User arg: ID {userField.Value.ReferenceID}");
                        break;
                    }

                    if (c is ValueField<bool> boolField)
                    {
                        args.Add(new Value { u32 = boolField.Value ? 1u : 0u });
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
            machine.RegisterReturningHostFunc<ulong, int, int, int, int>(
                "env",
                "component__get_member",
                component__get_member
            );
            machine.RegisterReturningHostFunc<ulong, int>(
                "env",
                "component__get_type_name",
                component__get_type_name
            );

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
                "slot__get_num_children",
                slot__get_num_children
            );
            machine.RegisterReturningHostFunc<ulong, int, ulong>(
                "env",
                "slot__get_child",
                slot__get_child
            );
            machine.RegisterReturningHostFunc<ulong, int, int, int, int, ulong>(
                "env",
                "slot__find_child_by_name",
                slot__find_child_by_name
            );
            machine.RegisterReturningHostFunc<ulong, int, int, ulong>(
                "env",
                "slot__find_child_by_tag",
                slot__find_child_by_tag
            );
            machine.RegisterReturningHostFunc<ulong, int, ulong>(
                "env",
                "slot__get_component",
                slot__get_component
            );

            void RegisterValueGetSet<T>(
                string name,
                Func<Frame, ulong, int, int> valueGet,
                Func<Frame, ulong, int, int> valueSet
            )
                where T : unmanaged
            {
                machine.RegisterReturningHostFunc<ulong, int, int>(
                    "env",
                    $"value__get_{name}",
                    value__get<T>
                );
                machine.RegisterReturningHostFunc<ulong, int, int>(
                    "env",
                    $"value__set_{name}",
                    value__set<T>
                );
            }

            void RegisterBlitValueGetSet<T>(string name)
                where T : unmanaged
            {
                RegisterValueGetSet<T>(name, value__get<T>, value__set<T>);
            }

            RegisterBlitValueGetSet<int>("int");
            RegisterBlitValueGetSet<float>("float");
            RegisterBlitValueGetSet<double>("double");
        }

        //
        // The host functions. They are always called from WASM, so they already have a frame.
        //

        public ulong slot__root_slot(Frame frame)
        {
            ISlot slot = worldServices.GetRootSlot();
            return (ulong)slot.ReferenceID;
        }

        public ulong slot__get_parent(Frame frame, ulong slot_id)
        {
            Slot slot = FromRefID<Slot>(slot_id);
            return ((ulong?)slot?.Parent?.ReferenceID) ?? 0;
        }

        public ulong slot__get_active_user(Frame frame, ulong slot_id)
        {
            Slot slot = FromRefID<Slot>(slot_id);
            return ((ulong?)slot?.ActiveUser?.ReferenceID) ?? 0;
        }

        public ulong slot__get_active_user_root(Frame frame, ulong slot_id)
        {
            Slot slot = FromRefID<Slot>(slot_id);
            return ((ulong?)slot?.ActiveUserRoot?.ReferenceID) ?? 0;
        }

        public ulong slot__get_object_root(Frame frame, ulong slot_id, int only_explicit)
        {
            Slot slot = FromRefID<Slot>(slot_id);
            return ((ulong?)slot?.GetObjectRoot(only_explicit != 0)?.ReferenceID) ?? 0;
        }

        public int slot__get_name(Frame frame, ulong slot_id)
        {
            Slot slot = FromRefID<Slot>(slot_id);
            string name = slot?.Name ?? "";
            return emscriptenEnv.AllocateUTF8StringInMem(frame, name);
        }

        public void slot__set_name(Frame frame, ulong slot_id, int ptr)
        {
            Slot slot = FromRefID<Slot>(slot_id);
            if (slot == null)
                return;
            slot.Name = emscriptenEnv.GetUTF8StringFromMem(ptr);
        }

        public int slot__get_num_children(Frame frame, ulong slot_id)
        {
            Slot slot = FromRefID<Slot>(slot_id);
            return slot?.ChildrenCount ?? 0;
        }

        public ulong slot__get_child(Frame frame, ulong slot_id, int index)
        {
            Slot slot = FromRefID<Slot>(slot_id);
            return ((ulong?)slot?[index]?.ReferenceID) ?? 0;
        }

        public ulong slot__find_child_by_name(
            Frame frame,
            ulong slot_id,
            int namePtr,
            int match_substring,
            int ignore_case,
            int max_depth
        )
        {
            Slot slot = FromRefID<Slot>(slot_id);
            string name = emscriptenEnv.GetUTF8StringFromMem(namePtr);
            return (
                    (ulong?)
                        slot?.FindChild(
                            name,
                            match_substring != 0,
                            ignore_case != 0,
                            max_depth
                        )?.ReferenceID
                ) ?? 0;
        }

        public ulong slot__find_child_by_tag(Frame frame, ulong slot_id, int tagPtr, int max_depth)
        {
            Slot slot = FromRefID<Slot>(slot_id);
            string tag = emscriptenEnv.GetUTF8StringFromMem(tagPtr);
            return ((ulong?)slot?.FindChild(s => s.Tag == tag, max_depth)?.ReferenceID) ?? 0;
        }

        public ulong slot__get_component(Frame frame, ulong slot_id, int typeNamePtr)
        {
            Slot slot = FromRefID<Slot>(slot_id);
            string typeName = emscriptenEnv.GetUTF8StringFromMem(typeNamePtr);
            Type type = Type.GetType(typeName);
            if (type == null)
                return 0;
            return ((ulong?)slot?.GetComponent(type)?.ReferenceID) ?? 0;
        }

        public int component__get_type_name(Frame frame, ulong component_id)
        {
            Component c = FromRefID<Component>(component_id);
            string typeName = c?.GetType().GetNiceName() ?? "";
            return emscriptenEnv.AllocateUTF8StringInMem(frame, typeName);
        }

        public enum ResoniteType : int
        {
            Unknown = 0x0,

            // TODO: Renumber this to actually make sense.
            ValueInt = 0x1,
        }

        private static ResoniteType GetResoniteType(Type type)
        {
            if (typeof(IValue<int>).IsAssignableFrom(type))
            {
                return ResoniteType.ValueInt;
            }
            return ResoniteType.Unknown;
        }

        public int component__get_member(
            Frame frame,
            ulong componentRefId,
            int namePtr,
            int outTypePtr,
            int outRefIdPtr
        )
        {
            Component component = FromRefID<Component>(componentRefId);
            if (component == null || namePtr == 0 || outTypePtr == 0 || outRefIdPtr == 0)
            {
                return -1;
            }

            string fieldName = emscriptenEnv.GetUTF8StringFromMem(namePtr);
            if (fieldName == null)
            {
                return -1;
            }

            var member = component.GetSyncMember(fieldName);
            if (member == null)
            {
                return -1;
            }

            machine.HeapSet<int>(outTypePtr, (int)GetResoniteType(member.GetType()));
            machine.HeapSet<ulong>(outRefIdPtr, (ulong)member.ReferenceID);
            return 0;
        }

        public int value__get<T>(Frame frame, ulong refId, int outPtr)
            where T : unmanaged
        {
            if (outPtr == 0)
            {
                return -1;
            }
            var value = FromRefID<IValue<T>>(refId);
            if (value == null)
            {
                return -1;
            }
            machine.HeapSet(outPtr, value.Value);
            return 0;
        }

        public int value__set<T>(Frame frame, ulong refId, int inPtr)
            where T : unmanaged
        {
            if (inPtr == 0)
            {
                return -1;
            }
            var value = FromRefID<IValue<T>>(refId);
            if (value == null)
            {
                return -1;
            }
            value.Value = machine.HeapGet<T>(inPtr);
            return 0;
        }
    }
}
