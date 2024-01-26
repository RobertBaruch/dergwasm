using System;
using Elements.Core;
using FrooxEngine;

namespace Derg
{
    // Implementation of ISlot using a real Slot. This should contain
    // whatever methods on Slot are needed, and the methods should be simple
    // passthroughs to the underlying Slot.
    //
    // Having to pass everything through this proxy is a small price to pay
    // for testability.
    public class SlotProxy : ISlot
    {
        Slot slot;

        public ISlot Parent
        {
            get => new SlotProxy(slot.Parent);
            set => slot.Parent = ((SlotProxy)value).slot;
        }

        public RefID ReferenceID
        {
            get => slot.ReferenceID;
        }

        public SlotProxy(Slot slot)
        {
            this.slot = slot;
        }

        public ISlot FindChild(Predicate<Slot> filter, int maxDepth = -1) =>
            new SlotProxy(slot.FindChild(filter, maxDepth));

        public ISlot FindChild(string name) => new SlotProxy(slot.FindChild(name));

        public T GetComponent<T>(Predicate<T> filter = null, bool excludeDisabled = false)
            where T : class => slot.GetComponent(filter, excludeDisabled);

        public ISlot AddSlot(string name = "Slot", bool persistent = true) =>
            new SlotProxy(slot.AddSlot(name, persistent));

        public T AttachComponent<T>(bool runOnAttachBehavior = true, Action<T> beforeAttach = null)
            where T : Component, new() => slot.AttachComponent(runOnAttachBehavior, beforeAttach);

        public void Destroy() => slot.Destroy();
    }
}
