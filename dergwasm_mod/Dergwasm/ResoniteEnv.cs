using System;
using System.Collections.Generic;
using Derg.Wasm;
using Elements.Core;
using FrooxEngine;

namespace Derg
{
    public enum ResoniteError : int
    {
        Success = 0,
        NullArgument = -1,
        InvalidRefId = -2,
    }

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

        public T FromRefID<T>(RefID slot_id)
            where T : class, IWorldElement
        {
            return worldServices.GetObjectOrNull(slot_id) as T;
        }

        public IWorldElement FromRefID(RefID slot_id)
        {
            return worldServices.GetObjectOrNull(slot_id);
        }

        List<Value> ExtractArgs(Slot argsSlot, List<Ptr> allocations)
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
                        var buf = emscriptenEnv.AllocateUTF8StringInMem(null, stringField.Value);
                        allocations.Add(buf.Ptr);
                        args.Add(new Value { s32 = buf.Ptr.Addr });
                        DergwasmMachine.Msg($"String arg, ptr = 0x{buf.Ptr.Addr:X8}");
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
            List<Ptr> argAllocations = new List<Ptr>();
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
                foreach (Ptr ptr in argAllocations)
                {
                    emscriptenEnv.free(emscriptenEnv.EmptyFrame(), ptr.Addr);
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
            machine.RegisterReturningHostFunc<
                WasmRefID<Component>,
                Ptr<byte>,
                Ptr<int>,
                Ptr<ulong>,
                int
            >("env", "component__get_member", component__get_member);
            machine.RegisterReturningHostFunc<WasmRefID<Component>, Ptr<byte>>(
                "env",
                "component__get_type_name",
                component__get_type_name
            );

            machine.RegisterReturningHostFunc("env", "slot__root_slot", slot__root_slot);
            machine.RegisterReturningHostFunc<WasmRefID<ISlot>, WasmRefID<ISlot>>(
                "env",
                "slot__get_parent",
                slot__get_parent
            );
            machine.RegisterReturningHostFunc<WasmRefID<ISlot>, WasmRefID<User>>(
                "env",
                "slot__get_active_user",
                slot__get_active_user
            );
            machine.RegisterReturningHostFunc<WasmRefID<ISlot>, WasmRefID<UserRoot>>(
                "env",
                "slot__get_active_user_root",
                slot__get_active_user_root
            );
            machine.RegisterReturningHostFunc<WasmRefID<ISlot>, int, WasmRefID<ISlot>>(
                "env",
                "slot__get_object_root",
                slot__get_object_root
            );
            machine.RegisterReturningHostFunc<WasmRefID<ISlot>, Ptr<byte>>(
                "env",
                "slot__get_name",
                slot__get_name
            );
            machine.RegisterVoidHostFunc<WasmRefID<ISlot>, Ptr<byte>>(
                "env",
                "slot__set_name",
                slot__set_name
            );
            machine.RegisterReturningHostFunc<WasmRefID<ISlot>, int>(
                "env",
                "slot__get_num_children",
                slot__get_num_children
            );
            machine.RegisterReturningHostFunc<WasmRefID<ISlot>, Buff<WasmRefID>>(
                "env",
                "slot__get_children",
                slot__get_children
            );
            machine.RegisterReturningHostFunc<WasmRefID<ISlot>, int, WasmRefID<ISlot>>(
                "env",
                "slot__get_child",
                slot__get_child
            );
            machine.RegisterReturningHostFunc<
                WasmRefID<ISlot>,
                Ptr<byte>,
                int,
                int,
                int,
                WasmRefID<ISlot>
            >("env", "slot__find_child_by_name", slot__find_child_by_name);
            machine.RegisterReturningHostFunc<WasmRefID<ISlot>, Ptr<byte>, int, WasmRefID<ISlot>>(
                "env",
                "slot__find_child_by_tag",
                slot__find_child_by_tag
            );
            machine.RegisterReturningHostFunc<WasmRefID<ISlot>, Ptr<byte>, WasmRefID<Component>>(
                "env",
                "slot__get_component",
                slot__get_component
            );

            void RegisterValueGetSet<T>(
                string name,
                Func<Frame, WasmRefID<IValue<T>>, Ptr<T>, ResoniteError> valueGet,
                Func<Frame, WasmRefID<IValue<T>>, Ptr<T>, ResoniteError> valueSet
            )
                where T : unmanaged
            {
                machine.RegisterReturningHostFunc<WasmRefID<IValue<T>>, Ptr<T>, ResoniteError>(
                    "env",
                    $"value__get_{name}",
                    value__get<T>
                );
                machine.RegisterReturningHostFunc<WasmRefID<IValue<T>>, Ptr<T>, ResoniteError>(
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

        public WasmRefID<ISlot> slot__root_slot(Frame frame)
        {
            return new WasmRefID<ISlot>(worldServices.GetRootSlot());
        }

        public WasmRefID<ISlot> slot__get_parent(Frame frame, WasmRefID<ISlot> slot)
        {
            return worldServices.GetObjectOrNull(slot)?.Parent?.GetWasmRef() ?? default;
        }

        public WasmRefID<User> slot__get_active_user(Frame frame, WasmRefID<ISlot> slot)
        {
            return worldServices.GetObjectOrNull(slot)?.ActiveUser?.GetWasmRef() ?? default;
        }

        public WasmRefID<UserRoot> slot__get_active_user_root(Frame frame, WasmRefID<ISlot> slot)
        {
            return worldServices.GetObjectOrNull(slot)?.ActiveUserRoot?.GetWasmRef() ?? default;
        }

        public WasmRefID<ISlot> slot__get_object_root(
            Frame frame,
            WasmRefID<ISlot> slot,
            int only_explicit
        )
        {
            return worldServices
                    .GetObjectOrNull(slot)
                    ?.GetObjectRoot(only_explicit != 0)
                    ?.GetWasmRef() ?? default;
        }

        public Ptr<byte> slot__get_name(Frame frame, WasmRefID<ISlot> slot)
        {
            string name = worldServices.GetObjectOrNull(slot)?.Name ?? "";
            return emscriptenEnv.AllocateUTF8StringInMem(frame, name).ToPointer();
        }

        public void slot__set_name(Frame frame, WasmRefID<ISlot> slot, Ptr<byte> ptr)
        {
            ISlot islot = worldServices.GetObjectOrNull(slot);
            if (islot == null)
                return;
            islot.Name = emscriptenEnv.GetUTF8StringFromMem(ptr);
        }

        public int slot__get_num_children(Frame frame, WasmRefID<ISlot> slot)
        {
            return worldServices.GetObjectOrNull(slot)?.ChildrenCount ?? 0;
        }

        // Gets a list of the children of the given slot, returning a pointer to a buffer
        // of reference IDs. The caller is responsible for freeing
        // the memory allocated for the list.
        public Buff<WasmRefID> slot__get_children(Frame frame, WasmRefID<ISlot> slot)
        {
            ISlot s = worldServices.GetObjectOrNull(slot);
            if (s == null)
                return default;
            Buff<WasmRefID> buffer = emscriptenEnv.Malloc<WasmRefID>(frame, s.ChildrenCount);
            Ptr<WasmRefID> ptr = buffer.Ptr;
            foreach (ISlot child in s.Children)
            {
                WasmRefID<ISlot> refID = child.GetWasmRef();
                machine.HeapSet(ptr, refID);
                ptr++;
            }
            return buffer;
        }

        public WasmRefID<ISlot> slot__get_child(Frame frame, WasmRefID<ISlot> slot, int index)
        {
            return worldServices.GetObjectOrNull(slot)?[index]?.GetWasmRef() ?? default;
        }

        public WasmRefID<ISlot> slot__find_child_by_name(
            Frame frame,
            WasmRefID<ISlot> slot,
            Ptr<byte> namePtr,
            int match_substring,
            int ignore_case,
            int max_depth
        )
        {
            string name = emscriptenEnv.GetUTF8StringFromMem(namePtr);
            return worldServices
                    .GetObjectOrNull(slot)
                    ?.FindChild(name, match_substring != 0, ignore_case != 0, max_depth)
                    ?.GetWasmRef() ?? default;
        }

        public WasmRefID<ISlot> slot__find_child_by_tag(
            Frame frame,
            WasmRefID<ISlot> slot,
            Ptr<byte> tagPtr,
            int max_depth
        )
        {
            string tag = emscriptenEnv.GetUTF8StringFromMem(tagPtr);
            return worldServices
                    .GetObjectOrNull(slot)
                    ?.FindChild(s => s.Tag == tag, max_depth)
                    ?.GetWasmRef() ?? default;
        }

        public WasmRefID<Component> slot__get_component(
            Frame frame,
            WasmRefID<ISlot> slot,
            Ptr<byte> typeNamePtr
        )
        {
            string typeName = emscriptenEnv.GetUTF8StringFromMem(typeNamePtr);
            Type type = Type.GetType(typeName);
            if (type == null)
                return default;
            return worldServices.GetObjectOrNull(slot)?.GetComponent(type)?.GetWasmRef() ?? default;
        }

        public Ptr<byte> component__get_type_name(Frame frame, WasmRefID<Component> component_id)
        {
            string typeName =
                worldServices.GetObjectOrNull(component_id)?.GetType().GetNiceName() ?? "";
            return emscriptenEnv.AllocateUTF8StringInMem(frame, typeName).Ptr;
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
            WasmRefID<Component> componentRefId,
            Ptr<byte> namePtr,
            Ptr<int> outTypePtr,
            Ptr<ulong> outRefIdPtr
        )
        {
            Component component = worldServices.GetObjectOrNull(componentRefId);
            if (
                component == null
                || namePtr.Addr == 0
                || outTypePtr.Addr == 0
                || outRefIdPtr.Addr == 0
            )
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

            machine.HeapSet(outTypePtr, (int)GetResoniteType(member.GetType()));
            machine.HeapSet(outRefIdPtr, (ulong)member.ReferenceID);
            return 0;
        }

        public ResoniteError value__get<T>(Frame frame, WasmRefID<IValue<T>> refId, Ptr<T> outPtr)
            where T : unmanaged
        {
            if (outPtr.IsNull)
            {
                return ResoniteError.NullArgument;
            }
            var value = worldServices.GetObjectOrNull(refId);
            if (value == null)
            {
                return ResoniteError.InvalidRefId;
            }
            machine.HeapSet(outPtr, value.Value);
            return ResoniteError.Success;
        }

        public ResoniteError value__set<T>(Frame frame, WasmRefID<IValue<T>> refId, Ptr<T> inPtr)
            where T : unmanaged
        {
            if (inPtr.IsNull)
            {
                return ResoniteError.NullArgument;
            }
            var value = worldServices.GetObjectOrNull(refId);
            if (value == null)
            {
                return ResoniteError.InvalidRefId;
            }
            value.Value = machine.HeapGet(inPtr);
            return ResoniteError.Success;
        }
    }
}
