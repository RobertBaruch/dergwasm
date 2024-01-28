using System;
using System.Collections.Generic;
using System.Linq;
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

        // Use this to construct SlotProxy instances. It will return null if
        // the underlying slot is null.
        public static SlotProxy SlotProxyFromSlot(Slot slot) =>
            slot == null ? null : new SlotProxy { slot = slot };

        public ISlot Parent
        {
            get => SlotProxyFromSlot(slot.Parent);
            set => slot.Parent = ((SlotProxy)value).slot;
        }

        IWorldElement IWorldElement.Parent
        {
            get
            {
                IWorldElement parent = Parent;
                if (parent.GetType() == typeof(Slot))
                    return SlotProxyFromSlot((Slot)parent);
                return parent;
            }
        }

        public RefID ReferenceID => slot.ReferenceID;

        public string Name
        {
            get => slot.Name;
            set => slot.Name = value;
        }

        public string Tag
        {
            get => slot.Tag;
            set => slot.Tag = value;
        }

        public UserRoot ActiveUserRoot => slot.ActiveUserRoot;

        public User ActiveUser => slot.ActiveUser;

        public World World => slot.World;

        public bool IsLocalElement => slot.IsLocalElement;

        public bool IsPersistent => slot.IsPersistent;

        public bool IsRemoved => slot.IsRemoved;

        public IEnumerable<ISlot> Children => slot.Children.Select(s => SlotProxyFromSlot(s));

        public ISlot FindChild(Predicate<ISlot> filter, int maxDepth = -1)
        {
            Predicate<Slot> slotFilter = (s) => filter(SlotProxyFromSlot(s));
            return SlotProxyFromSlot(slot.FindChild(slotFilter, maxDepth));
        }

        public ISlot FindChild(
            string name,
            bool matchSubstring,
            bool ignoreCase,
            int maxDepth = -1
        ) => SlotProxyFromSlot(slot.FindChild(name, matchSubstring, ignoreCase, maxDepth));

        public int ChildrenCount => slot.ChildrenCount;

        public ISlot this[int childIndex] => SlotProxyFromSlot(slot[childIndex]);

        public ISlot FindChild(string name) => SlotProxyFromSlot(slot.FindChild(name));

        public T GetComponent<T>(Predicate<T> filter = null, bool excludeDisabled = false)
            where T : class => slot.GetComponent(filter, excludeDisabled);

        public Component GetComponent(Type type, bool exactTypeOnly = false) =>
            slot.GetComponent(type, exactTypeOnly);

        public ISlot AddSlot(string name = "Slot", bool persistent = true) =>
            SlotProxyFromSlot(slot.AddSlot(name, persistent));

        public T AttachComponent<T>(bool runOnAttachBehavior = true, Action<T> beforeAttach = null)
            where T : Component, new() => slot.AttachComponent(runOnAttachBehavior, beforeAttach);

        public ISlot GetObjectRoot(bool explicitOnly = false) =>
            SlotProxyFromSlot(slot.GetObjectRoot(explicitOnly));

        public void Destroy() => slot.Destroy();

        // This is private in Slot, so we'd never call it.
        void IWorldElement.ChildChanged(IWorldElement child) => throw new NotImplementedException();

        public DataTreeNode Save(SaveControl control) => slot.Save(control);

        public void Load(DataTreeNode node, LoadControl control) => slot.Load(node, control);

        public string GetSyncMemberName(ISyncMember member) => slot.GetSyncMemberName(member);
    }
}
