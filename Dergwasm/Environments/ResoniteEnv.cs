﻿using System;
using System.Collections.Generic;
using Dergwasm.Modules;
using Dergwasm.Resonite;
using Dergwasm.Wasm;
using Dergwasm.Runtime;
using Elements.Core;
using FrooxEngine;
using System.Linq;

namespace Dergwasm.Environments
{
    // Provides the functions in resonite_api.h. A ResoniteEnv, like a Machine, is specific
    // to a World.
    //
    // In the API, we don't use anything other than ints, longs, floats, and doubles.
    // Pointers to memory are uints.
    [Mod("resonite")]
    public class ResoniteEnv : ReflectedModule
    {
        public Machine machine;
        public IWorld world;
        public EmscriptenEnv emscriptenEnv;

        public ResoniteEnv(Machine machine, IWorld world, EmscriptenEnv emscriptenEnv)
        {
            this.machine = machine;
            this.world = world;
            this.emscriptenEnv = emscriptenEnv;
        }

        public T FromRefID<T>(RefID slot_id)
            where T : class, IWorldElement
        {
            return world.GetObjectOrNull(slot_id) as T;
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
        public ResoniteError slot__root_slot(Frame frame, Output<WasmRefID<Slot>> outSlot)
        {
            try
            {
                outSlot.CheckNullArg("outSlot");
                machine.HeapSet(outSlot, new WasmRefID<Slot>(world.GetRootSlot()));
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
            Output<WasmRefID<Slot>> outParent
        )
        {
            try
            {
                outParent.CheckNullArg("outParent");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

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
            Output<WasmRefID<User>> outUser
        )
        {
            try
            {
                outUser.CheckNullArg("outUser");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

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
            Output<WasmRefID<UserRoot>> outUserRoot
        )
        {
            try
            {
                outUserRoot.CheckNullArg("outUserRoot");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

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
            Output<WasmRefID<Slot>> outObjectRoot
        )
        {
            try
            {
                outObjectRoot.CheckNullArg("outObjectRoot");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

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
            Output<NullTerminatedString> outName
        )
        {
            try
            {
                outName.CheckNullArg("outName");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

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
                slot.CheckValidRef("slot", world, out Slot slotInstance);
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
            Output<int> outNumChildren
        )
        {
            try
            {
                outNumChildren.CheckNullArg("outNumChildren");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

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
            Output<WasmRefID<Slot>> outChild
        )
        {
            try
            {
                outChild.CheckNullArg("outChild");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

                machine.HeapSet(outChild, new WasmRefID<Slot>(slotInstance[index]));
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }

        // Gets a list of child ref IDs for the given slot. The caller is responsible for
        // freeing the data at outChildListData.
        [ModFn("slot__get_children")]
        public ResoniteError slot__get_components(
            Frame frame,
            WasmRefID<Slot> slot,
            Output<Buff<WasmRefID<Slot>>> outChildren
        )
        {
            try
            {
                outChildren.CheckNullArg("outChildren");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

                Buff<WasmRefID<Slot>> list = Buff<WasmRefID<Slot>>.Make(
                    machine,
                    frame,
                    slotInstance.Children.Select(e => e.GetWasmRef()).ToList()
                );
                machine.HeapSet(outChildren, list);
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
            Output<WasmRefID<Slot>> outChild
        )
        {
            try
            {
                name.CheckNullArg("name", emscriptenEnv, out string searchName);
                outChild.CheckNullArg("outChild");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

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
            Output<WasmRefID<Slot>> outChild
        )
        {
            try
            {
                tag.CheckNullArg("tag", emscriptenEnv, out string tagName);
                outChild.CheckNullArg("outChild");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

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
            NullTerminatedString typeName,
            Output<WasmRefID<Component>> outComponent
        )
        {
            try
            {
                outComponent.CheckNullArg("outComponent");
                slot.CheckValidRef("slot", world, out Slot slotInstance);
                typeName.CheckNullArg("typeName", emscriptenEnv, out string typeNameStr);
                Type type = Type.GetType(typeNameStr);
                if (type == null)
                {
                    throw new ResoniteException(
                        ResoniteError.FailedPrecondition,
                        $"No such type: {typeNameStr}"
                    );
                }
                machine.HeapSet(
                    outComponent,
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
            Output<Buff<WasmRefID<Component>>> outComponents
        )
        {
            try
            {
                outComponents.CheckNullArg("outComponents");
                slot.CheckValidRef("slot", world, out Slot slotInstance);

                Buff<WasmRefID<Component>> list = Buff<WasmRefID<Component>>.Make(
                    machine,
                    frame,
                    slotInstance.Components.Select(e => e.GetWasmRef()).ToList()
                );
                machine.HeapSet(outComponents, list);
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
            Output<NullTerminatedString> outTypeName
        )
        {
            try
            {
                outTypeName.CheckNullArg("outTypeName");
                component.CheckValidRef("component", world, out Component componentInstance);
                machine.HeapSet(
                    emscriptenEnv,
                    frame,
                    outTypeName,
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
            WasmRefID<Component> component,
            NullTerminatedString name,
            Output<ResoniteType> outType,
            Output<WasmRefID> outMember
        )
        {
            try
            {
                name.CheckNullArg("name", emscriptenEnv, out string fieldName);
                outType.CheckNullArg("outType");
                outMember.CheckNullArg("outMember");
                component.CheckValidRef("component", world, out Component componentInstance);

                var member = componentInstance.GetSyncMember(fieldName);
                if (member == null)
                {
                    throw new ResoniteException(ResoniteError.FailedPrecondition, "No such member");
                }

                machine.HeapSet(outType, GetResoniteType(member.GetType()));
                machine.HeapSet(outMember, new WasmRefID(member));
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
        public ResoniteError value__get<T>(
            Frame frame,
            WasmRefID<IValue<T>> refId,
            Output<T> outPtr
        )
            where T : unmanaged
        {
            try
            {
                outPtr.CheckNullArg("outPtr");
                refId.CheckValidRef("refId", world, out IValue<T> field);

                machine.HeapSet(outPtr, field.Value);
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
        public ResoniteError value__set<T>(Frame frame, WasmRefID<IValue<T>> refId, T value)
            where T : unmanaged
        {
            try
            {
                refId.CheckValidRef("refId", world, out IValue<T> field);

                field.Value = value;
            }
            catch (Exception e)
            {
                return e.ToError();
            }
            return default;
        }
    }
}
