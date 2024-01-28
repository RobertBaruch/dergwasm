using System;
using System.Collections.Generic;
using FrooxEngine;

namespace Derg
{
    // An interface for a slot in a world, for testability.
    public interface ISlot : IWorldElement
    {
        new ISlot Parent { get; set; }

        new string Name { get; set; }

        string Tag { get; set; }

        UserRoot ActiveUserRoot { get; }

        User ActiveUser { get; }

        int ChildrenCount { get; }

        IEnumerable<ISlot> Children { get; }

        ISlot this[int childIndex] { get; }

        ISlot FindChild(Predicate<ISlot> filter, int maxDepth = -1);

        ISlot FindChild(string name);

        ISlot FindChild(string name, bool matchSubstring, bool ignoreCase, int maxDepth = -1);

        T GetComponent<T>(Predicate<T> filter = null, bool excludeDisabled = false)
            where T : class;

        Component GetComponent(Type type, bool exactTypeOnly = false);

        ISlot AddSlot(string name = "Slot", bool persistent = true);

        T AttachComponent<T>(bool runOnAttachBehavior = true, Action<T> beforeAttach = null)
            where T : Component, new();

        ISlot GetObjectRoot(bool explicitOnly = false);

        void Destroy();
    }
}
