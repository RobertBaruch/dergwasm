using System;
using Elements.Core;
using FrooxEngine;

namespace Derg
{
    // An interface for a slot in a world, for testability.
    public interface ISlot
    {
        ISlot Parent { get; set; }
        RefID ReferenceID { get; }
        ISlot FindChild(Predicate<Slot> filter, int maxDepth = -1);

        ISlot FindChild(string name);

        T GetComponent<T>(Predicate<T> filter = null, bool excludeDisabled = false)
            where T : class;

        ISlot AddSlot(string name = "Slot", bool persistent = true);

        T AttachComponent<T>(bool runOnAttachBehavior = true, Action<T> beforeAttach = null)
            where T : Component, new();

        void Destroy();
    }
}
