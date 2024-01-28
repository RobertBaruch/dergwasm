using System;
using System.Collections.Generic;
using Derg.Modules;
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
    [Mod("env")]
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

        public T FromRefID<T>(RefID slot_id)
            where T : class, IWorldElement
        {
            return worldServices.GetObjectOrNull(slot_id) as T;
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

        [ModFn("slot__root_slot")]
        public WasmRefID<ISlot> slot__root_slot(Frame frame)
        {
            return new WasmRefID<ISlot>(worldServices.GetRootSlot().ReferenceID);
        }

        [ModFn("slot__get_parent")]
        public WasmRefID<ISlot> slot__get_parent(Frame frame, WasmRefID<ISlot> slot)
        {
            return worldServices.GetObjectOrNull(slot)?.Parent?.GetWasmRef() ?? default;
        }

        [ModFn("slot__get_active_user")]
        public WasmRefID<User> slot__get_active_user(Frame frame, WasmRefID<ISlot> slot)
        {
            return worldServices.GetObjectOrNull(slot)?.ActiveUser?.GetWasmRef() ?? default;
        }

        [ModFn("slot__get_active_user_root")]
        public WasmRefID<UserRoot> slot__get_active_user_root(Frame frame, WasmRefID<ISlot> slot)
        {
            return worldServices.GetObjectOrNull(slot)?.ActiveUserRoot?.GetWasmRef() ?? default;
        }

        [ModFn("slot__get_object_root")]
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

        [ModFn("slot__get_name")]
        public int slot__get_name(Frame frame, WasmRefID<ISlot> slot)
        {
            string name = worldServices.GetObjectOrNull(slot)?.Name ?? "";
            return emscriptenEnv.AllocateUTF8StringInMem(frame, name).Ptr.Addr;
        }

        [ModFn("slot__set_name")]
        public void slot__set_name(Frame frame, WasmRefID<ISlot> slot, int ptr)
        {
            ISlot islot = worldServices.GetObjectOrNull(slot);
            if (islot == null)
                return;
            islot.Name = emscriptenEnv.GetUTF8StringFromMem(ptr);
        }

        [ModFn("slot__get_num_children")]
        public int slot__get_num_children(Frame frame, WasmRefID<ISlot> slot)
        {
            return worldServices.GetObjectOrNull(slot)?.ChildrenCount ?? 0;
        }

        [ModFn("slot__get_child")]
        public WasmRefID<ISlot> slot__get_child(Frame frame, WasmRefID<ISlot> slot, int index)
        {
            return worldServices.GetObjectOrNull(slot)?[index]?.GetWasmRef() ?? default;
        }

        [ModFn("slot__find_child_by_name")]
        public WasmRefID<ISlot> slot__find_child_by_name(
            Frame frame,
            WasmRefID<ISlot> slot,
            int namePtr,
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

        [ModFn("slot__find_child_by_tag")]
        public WasmRefID<ISlot> slot__find_child_by_tag(
            Frame frame,
            WasmRefID<ISlot> slot,
            int tagPtr,
            int max_depth
        )
        {
            string tag = emscriptenEnv.GetUTF8StringFromMem(tagPtr);
            return worldServices
                    .GetObjectOrNull(slot)
                    ?.FindChild(s => s.Tag == tag, max_depth)
                    ?.GetWasmRef() ?? default;
        }

        [ModFn("slot__get_component")]
        public WasmRefID<Component> slot__get_component(
            Frame frame,
            WasmRefID<ISlot> slot,
            int typeNamePtr
        )
        {
            string typeName = emscriptenEnv.GetUTF8StringFromMem(typeNamePtr);
            Type type = Type.GetType(typeName);
            if (type == null)
                return default;
            return worldServices.GetObjectOrNull(slot)?.GetComponent(type)?.GetWasmRef() ?? default;
        }

        [ModFn("component__get_type_name")]
        public int component__get_type_name(Frame frame, ulong component_id)
        {
            Component c = FromRefID<Component>(component_id);
            string typeName = c?.GetType().GetNiceName() ?? "";
            return emscriptenEnv.AllocateUTF8StringInMem(frame, typeName).Ptr.Addr;
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

        [ModFn("component__get_member")]
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

            machine.HeapSet(new Ptr<int>(outTypePtr), (int)GetResoniteType(member.GetType()));
            machine.HeapSet(new Ptr<ulong>(outRefIdPtr), (ulong)member.ReferenceID);
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
