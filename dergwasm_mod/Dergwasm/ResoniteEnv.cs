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
    // FrooxEngine.World doesn't provide any methods to find objects by their ReferenceID.
    // Therefore, those have to be patched in.
    public class ResoniteEnv
    {
        public Machine machine;
        public World world;

        public ResoniteEnv(Machine machine, World world)
        {
            this.machine = machine;
        }

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

        public ulong slot__get_active_user(Frame frame, ulong slot_id)
        {
            Slot slot = world.GetSlotByReferenceID(slot_id); // TODO: Implement.
            if (slot == null)
            {
                return 0;
            }
            return (ulong)slot.ActiveUser.ReferenceID;
        }

        public ulong slot__get_active_user_root(Frame frame, ulong slot_id)
        {
            Slot slot = world.GetSlotByReferenceID(slot_id);
            if (slot == null)
            {
                return 0;
            }
            return (ulong)slot.ActiveUserRoot.ReferenceID;
        }

        public ulong slot__get_object_root(Frame frame, ulong slot_id, int only_explicit)
        {
            Slot slot = world.GetSlotByReferenceID(slot_id);
            if (slot == null)
            {
                return 0;
            }
            return (ulong)slot.GetObjectRoot(only_explicit != 0).ReferenceID;
        }

        public ulong slot__get_parent(Frame frame, ulong slot_id)
        {
            Slot slot = world.GetSlotByReferenceID(slot_id);
            if (slot == null)
            {
                return 0;
            }
            return (ulong)slot.Parent.ReferenceID;
        }
    }
}
