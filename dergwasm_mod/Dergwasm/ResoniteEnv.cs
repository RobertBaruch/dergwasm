using System;
using System.Collections.Generic;
using Derg.Modules;
using Derg.Resonite;
using Derg.Wasm;
using Elements.Core;
using FrooxEngine;

namespace Derg
{
    // Provides the functions in resonite_api.h. A ResoniteEnv, like a Machine, is specific
    // to a World.
    //
    // In the API, we don't use anything other than ints, longs, floats, and doubles.
    // Pointers to memory are uints.
    [Mod("env")]
    public class ResoniteEnv : ReflectedModule
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
        public void InvokeWasmFunction(string funcName, List<Value> args)
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
        // The host functions. They are always called from WASM, so they already have a frame.
        //

        [ModFn("slot__root_slot")]
        public ResoniteError slot__root_slot(Frame frame, Ptr<WasmRefID<Slot>> outSlot)
        {
            try
            {
                outSlot.CheckNullArg("outSlot");
                machine.HeapSet(outSlot, new WasmRefID<Slot>(worldServices.GetRootSlot()));
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("slot__get_parent")]
        public ResoniteError slot__get_parent(
            Frame frame,
            WasmRefID<Slot> slot,
            Ptr<WasmRefID<Slot>> outParent
        )
        {
            try
            {
                outParent.CheckNullArg("outParent");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);

                machine.HeapSet(outParent, new WasmRefID<Slot>(slotInstance.Parent));
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("slot__get_active_user")]
        public ResoniteError slot__get_active_user(
            Frame frame,
            WasmRefID<Slot> slot,
            Ptr<WasmRefID<User>> outUser
        )
        {
            try
            {
                outUser.CheckNullArg("outUser");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);

                machine.HeapSet(outUser, new WasmRefID<User>(slotInstance.ActiveUser));
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("slot__get_active_user_root")]
        public ResoniteError slot__get_active_user_root(
            Frame frame,
            WasmRefID<Slot> slot,
            Ptr<WasmRefID<UserRoot>> outUserRoot
        )
        {
            try
            {
                outUserRoot.CheckNullArg("outUserRoot");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);

                machine.HeapSet(outUserRoot, new WasmRefID<UserRoot>(slotInstance.ActiveUserRoot));
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("slot__get_object_root")]
        public ResoniteError slot__get_object_root(
            Frame frame,
            WasmRefID<Slot> slot,
            bool only_explicit,
            Ptr<WasmRefID<Slot>> outObjectRoot
        )
        {
            try
            {
                outObjectRoot.CheckNullArg("outObjectRoot");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);

                machine.HeapSet(
                    outObjectRoot,
                    new WasmRefID<Slot>(slotInstance.GetObjectRoot(only_explicit))
                );
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("slot__get_name")]
        public ResoniteError slot__get_name(
            Frame frame,
            WasmRefID<Slot> slot,
            Ptr<NullTerminatedString> outName
        )
        {
            try
            {
                outName.CheckNullArg("outName");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);

                machine.HeapSet(emscriptenEnv, frame, outName, slotInstance.Name);
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("slot__set_name")]
        public ResoniteError slot__set_name(
            Frame frame,
            WasmRefID<Slot> slot,
            NullTerminatedString name
        )
        {
            try
            {
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);
                // The name can be null, and that's ok.
                slotInstance.Name = emscriptenEnv.GetUTF8StringFromMem(name);
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("slot__get_num_children")]
        public ResoniteError slot__get_num_children(
            Frame frame,
            WasmRefID<Slot> slot,
            Ptr<int> outNumChildren
        )
        {
            try
            {
                outNumChildren.CheckNullArg("outNumChildren");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);

                machine.HeapSet(outNumChildren, slotInstance.ChildrenCount);
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("slot__get_child")]
        public ResoniteError slot__get_child(
            Frame frame,
            WasmRefID<Slot> slot,
            int index,
            Ptr<WasmRefID<Slot>> outChild
        )
        {
            try
            {
                outChild.CheckNullArg("outChild");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);

                machine.HeapSet(outChild, new WasmRefID<Slot>(slotInstance[index]));
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        // Finds a child slot by name. If no match was found, success is returned, but outChild
        // will be the null reference.
        [ModFn("slot__find_child_by_name")]
        public ResoniteError slot__find_child_by_name(
            Frame frame,
            WasmRefID<Slot> slot,
            NullTerminatedString name,
            bool match_substring,
            bool ignore_case,
            int max_depth,
            Ptr<WasmRefID<Slot>> outChild
        )
        {
            try
            {
                name.CheckNullArg("name", emscriptenEnv, out string searchName);
                outChild.CheckNullArg("outChild");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);

                machine.HeapSet(
                    outChild,
                    new WasmRefID<Slot>(
                        slotInstance.FindChild(searchName, match_substring, ignore_case, max_depth)
                    )
                );
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        // Finds a child slot by tag. If no match was found, success is returned, but outChild
        // will be the null reference.
        [ModFn("slot__find_child_by_tag")]
        public ResoniteError slot__find_child_by_tag(
            Frame frame,
            WasmRefID<Slot> slot,
            NullTerminatedString tag,
            int max_depth,
            Ptr<WasmRefID<Slot>> outChild
        )
        {
            try
            {
                tag.CheckNullArg("tag", emscriptenEnv, out string tagName);
                outChild.CheckNullArg("outChild");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);

                machine.HeapSet(
                    outChild,
                    new WasmRefID<Slot>(slotInstance.FindChild(s => s.Tag == tagName, max_depth))
                );
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("slot__get_component")]
        public ResoniteError slot__get_component(
            Frame frame,
            WasmRefID<Slot> slot,
            NullTerminatedString typeNamePtr,
            Ptr<WasmRefID<Component>> outComponentIdPtr
        )
        {
            try
            {
                outComponentIdPtr.CheckNullArg("componentIdPtr");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);
                typeNamePtr.CheckNullArg("typeNamePtr", emscriptenEnv, out string typeName);
                Type type = Type.GetType(typeName);
                if (type == null)
                {
                    throw new ResoniteException(
                        ResoniteError.FailedPrecondition,
                        $"No such type: {typeName}"
                    );
                }
                machine.HeapSet(
                    outComponentIdPtr,
                    new WasmRefID<Component>(slotInstance.GetComponent(type))
                );
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        // Gets a list of component ref IDs for the given slot. The caller is responsible for
        // freeing the data at outComponentListData.
        [ModFn("slot__get_components")]
        public ResoniteError slot__get_components(
            Frame frame,
            WasmRefID<Slot> slot,
            Ptr<int> outComponentListLength,
            Ptr<WasmArray<WasmRefID<Component>>> outComponentListData
        )
        {
            try
            {
                outComponentListLength.CheckNullArg("outComponentListLength");
                outComponentListData.CheckNullArg("outComponentListData");
                slot.CheckValidRef("slot", worldServices, out Slot slotInstance);

                WasmRefIDList<Component> list = WasmRefIDList<Component>.Make(
                    machine,
                    frame,
                    slotInstance.Components
                );
                machine.HeapSet(outComponentListLength, list.buff.Length);
                machine.HeapSet(
                    outComponentListData,
                    new WasmArray<WasmRefID<Component>>(list.buff.Ptr)
                );
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("component__get_type_name")]
        public ResoniteError component__get_type_name(
            Frame frame,
            WasmRefID<Component> component,
            Ptr<NullTerminatedString> outPtr
        )
        {
            try
            {
                outPtr.CheckNullArg("outPtr");
                component.CheckValidRef(
                    "component",
                    worldServices,
                    out Component componentInstance
                );
                machine.HeapSet(
                    emscriptenEnv,
                    frame,
                    outPtr,
                    componentInstance.GetType().GetNiceName()
                );
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
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
        public ResoniteError component__get_member(
            Frame frame,
            WasmRefID<Component> componentRefId,
            NullTerminatedString namePtr,
            Ptr<ResoniteType> outTypePtr,
            Ptr<ulong> outRefIdPtr
        )
        {
            try
            {
                namePtr.CheckNullArg("namePtr", emscriptenEnv, out string fieldName);
                outTypePtr.CheckNullArg("outTypePtr");
                outRefIdPtr.CheckNullArg("outRefIdPtr");
                componentRefId.CheckValidRef(
                    "componentRefId",
                    worldServices,
                    out Component component
                );

                var member = component.GetSyncMember(fieldName);
                if (member == null)
                {
                    throw new ResoniteException(ResoniteError.FailedPrecondition, "No such member");
                }

                machine.HeapSet(outTypePtr, GetResoniteType(member.GetType()));
                machine.HeapSet(outRefIdPtr, member);
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("value__get_int", typeof(int))]
        [ModFn("value__get_float", typeof(float))]
        [ModFn("value__get_double", typeof(double))]
        public ResoniteError value__get<T>(Frame frame, WasmRefID<IValue<T>> refId, Ptr<T> outPtr)
            where T : unmanaged
        {
            try
            {
                outPtr.CheckNullArg("outPtr");
                refId.CheckValidRef("refId", worldServices, out IValue<T> value);

                machine.HeapSet(outPtr, value.Value);
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        [ModFn("value__set_int", typeof(int))]
        [ModFn("value__set_float", typeof(float))]
        [ModFn("value__set_double", typeof(double))]
        public ResoniteError value__set<T>(Frame frame, WasmRefID<IValue<T>> refId, Ptr<T> inPtr)
            where T : unmanaged
        {
            try
            {
                inPtr.CheckNullArg("inPtr");
                refId.CheckValidRef("refId", worldServices, out IValue<T> value);

                value.Value = machine.HeapGet(inPtr);
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }
    }
}
