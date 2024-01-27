using System;
using System.Collections.Generic;
using System.Reflection;
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
        IWorldServices worldServices;
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
            machine.RegisterReturningHostFunc<ulong, int, int>(
                "env",
                "component__get_field_value",
                component__get_field_value
            );
            machine.RegisterReturningHostFunc<ulong, int>(
                "env",
                "component__get_type_name",
                component__get_type_name
            );
            machine.RegisterReturningHostFunc<ulong, int, int, int>(
                "env",
                "component__set_field_value",
                component__set_field_value
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

            machine.RegisterReturningHostFunc<ulong, int>(
                "env",
                "value_field__get_value",
                value_field__get_value
            );
            machine.RegisterReturningHostFunc<ulong, int, int>(
                "env",
                "value_field__set_value",
                value_field__set_value
            );

            machine.RegisterReturningHostFunc<ulong, ulong>(
                "env",
                "value_field_proxy__get_source",
                value_field_proxy__get_source
            );

            machine.RegisterReturningHostFunc<ulong, ulong, int>(
                "env",
                "value_field_proxy__set_source",
                value_field_proxy__set_source
            );
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

        // Gets the value of a field on a component. The field name is in the given string.
        // The value is serialized into an allocated area of the heap, and the pointer to it
        // is returned.
        //
        // Returns null if the field doesn't exist, couldn't be gotten, or the value couldn't
        // be serialized.
        //
        // Fields of components are Sync, SyncRef, or SyncDelegate.
        //
        // Sync<T> fields contain values, and are SyncField<T>.
        // SyncRef<T> fields are SyncField<RefID>, where a SyncField contains a field reference (?).
        // SyncDelegate fields are SyncField<WorldDelegate>.
        // A WorldDelegate is a {target RefID, method string, type} tuple.
        //
        // Some fields are actually properties.
        //
        // There are also SyncLists.
        //
        // Currently we only support properties, Sync, and SyncRef.
        public int component__get_field_value(Frame frame, ulong component_id, int namePtr)
        {
            string fieldName = emscriptenEnv.GetUTF8StringFromMem(namePtr);
            Component component = FromRefID<Component>(component_id);
            object value;
            if (!ComponentUtils.GetFieldValue(component, fieldName, out value))
                return 0;

            return SimpleSerialization.Serialize(machine, this, frame, value);
        }

        // Sets the value of a field on a component. The field name is in the given string.
        // The value is deserialized from memory. Returns 0 on success, or -1 if the field
        // doesn't exist, couldn't be set, or the value couldn't be deserialized.
        public int component__set_field_value(
            Frame frame,
            ulong component_id,
            int namePtr,
            int dataPtr
        )
        {
            string fieldName = emscriptenEnv.GetUTF8StringFromMem(namePtr);
            object value = SimpleSerialization.Deserialize(machine, this, dataPtr);
            if (value == null)
                return -1;

            Component component = FromRefID<Component>(component_id);
            if (!ComponentUtils.SetFieldValue(component, fieldName, value))
                return -1;
            return 0;
        }

        // Gets the value of a ValueField. The value is serialized into an allocated area of
        // the heap, and the pointer to it is returned.
        //
        // Returns null if the value couldn't be gotten or the value couldn't be serialized.
        public int value_field__get_value(Frame frame, ulong component_id)
        {
            IValueSource component = FromRefID<IValueSource>(component_id);
            if (!(component?.GetType()?.IsOfGenericType(typeof(ValueField<>)) ?? false))
                return 0;
            object value = component.BoxedValue;

            return SimpleSerialization.Serialize(machine, this, frame, value);
        }

        // Sets the value of a ValueField. The value is deserialized from memory.
        //
        // Returns 0 on success, or -1 if the component doesn't exist, the value
        // couldn't be deserialized, or the value couldn't be set.
        public int value_field__set_value(Frame frame, ulong component_id, int dataPtr)
        {
            Component component = FromRefID<Component>(component_id);
            if (!(component?.GetType()?.IsOfGenericType(typeof(ValueField<>)) ?? false))
                return -1;

            object value = SimpleSerialization.Deserialize(machine, this, dataPtr);
            if (!ComponentUtils.SetFieldValue(component, "Value", value))
                return -1;

            return 0;
        }

        // Returns the RefID of the field pointed to by the ValueFieldProxy component.
        public ulong value_field_proxy__get_source(Frame frame, ulong component_id)
        {
            Component component = FromRefID<Component>(component_id);
            if (!(component?.GetType()?.IsOfGenericType(typeof(ValueFieldProxy<>)) ?? false))
                return 0;

            object source;
            if (!ComponentUtils.GetFieldValue(component, "Source", out source))
                return 0;
            IField field = source as IField;
            return (ulong?)field?.ReferenceID ?? 0;
        }

        // Sets the RefID for the field pointed to by the ValueFieldProxy component.
        //
        // Returns 0 on success, or -1 if the component doesn't exist.
        public int value_field_proxy__set_source(Frame frame, ulong component_id, ulong value)
        {
            Component component = FromRefID<Component>(component_id);
            if (!(component?.GetType()?.IsOfGenericType(typeof(ValueFieldProxy<>)) ?? false))
                return -1;

            IField field = FromRefID<IField>(value);
            if (!ComponentUtils.SetFieldValue(component, "Source", field))
                return -1;
            return 0;
        }

        // Returns the value of the field pointed to by the ValueFieldProxy component.
        // The value is serialized into an allocated area of the heap, and the pointer to it
        // is returned.
        //
        // Returns null if the component doesn't exist, its source couldn't be gotten, or
        // the value couldn't be serialized.
        public int value_field_proxy__get_value(Frame frame, ulong component_id)
        {
            Component component = FromRefID<Component>(component_id);
            if (!(component?.GetType()?.IsOfGenericType(typeof(ValueFieldProxy<>)) ?? false))
                return 0;

            object value;
            if (!ComponentUtils.GetFieldValue(component, "Value", out value))
                return 0;

            return SimpleSerialization.Serialize(machine, this, frame, value);
        }

        public int value_field_proxy__set_value(Frame frame, ulong component_id, int dataPtr)
        {
            Component component = FromRefID<Component>(component_id);
            if (!(component?.GetType()?.IsOfGenericType(typeof(ValueFieldProxy<>)) ?? false))
                return -1;

            object source;
            if (!ComponentUtils.GetFieldValue(component, "Source", out source))
                return -1;
            IField field = source as IField;

            object value = SimpleSerialization.Deserialize(machine, this, dataPtr);
            if (value == null)
                return -1;

            // TODO: Should we also check CanWrite?
            try
            {
                field.BoxedValue = value;
            }
            catch (Exception e)
            {
                DergwasmMachine.Msg(
                    $"Failed to set ValueFieldProxy value on object of type {component.GetType()}: {e}"
                );
                return -1;
            }
            return 0;
        }
    }
}
