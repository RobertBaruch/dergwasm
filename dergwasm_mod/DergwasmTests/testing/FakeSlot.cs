using System;
using System.Collections.Generic;
using System.Reflection;
using Derg;
using Elements.Core;
using FrooxEngine;

namespace DergwasmTests.testing
{
    public class FakeSlot : ISlot
    {
        FakeWorldServices worldServices;
        List<ISlot> children = new List<ISlot>();
        List<Component> components = new List<Component>();

        public FakeSlot(FakeWorldServices worldServices, string name, ISlot parent = null)
        {
            this.worldServices = worldServices;
            Name = name;
            ReferenceID = worldServices.GetNextRefID();
            worldServices.AddRefID(this, ReferenceID);
            Parent = parent;
        }

        public ISlot Parent { get; set; }

        public RefID ReferenceID { get; set; }

        public string Name { get; set; }

        public string Tag { get; set; }

        public UserRoot ActiveUserRoot { get; set; }

        public User ActiveUser => ActiveUserRoot?.ActiveUser;

        public World World => throw new NotImplementedException();

        public bool IsLocalElement => throw new NotImplementedException();

        public bool IsPersistent => throw new NotImplementedException();

        public bool IsRemoved => throw new NotImplementedException();

        // In the real implementation, the parent of the root slot is the world.
        IWorldElement IWorldElement.Parent => Parent;

        public ISlot AddSlot(string name = "Slot", bool persistent = true)
        {
            var slot = new FakeSlot(worldServices, name, this);
            children.Add(slot);
            slot.Parent = this;
            return slot;
        }

        void Initialize(Component component)
        {
            component
                .GetType()
                .GetMethod("InitializeSyncMembers", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(component, new object[] { });
            component
                .GetType()
                .GetMethod("OnAwake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(component, new object[] { });
        }

        void SetRefId(IWorldElement obj, RefID i)
        {
            // This nonsense is required because a WorldElement's ReferenceID has a private setter
            // in a base class.
            PropertyInfo propertyInfo = obj.GetType().GetProperty("ReferenceID");
            var setterMethod = propertyInfo.GetSetMethod(true);
            if (setterMethod == null)
                setterMethod = propertyInfo
                    .DeclaringType
                    .GetProperty("ReferenceID")
                    .GetSetMethod(true);
            setterMethod.Invoke(obj, new object[] { i });
            worldServices.AddRefID(obj, i);
        }

        public T AttachComponent<T>(bool runOnAttachBehavior = true, Action<T> beforeAttach = null)
            where T : Component, new()
        {
            T component = new T();
            components.Add(component);
            Initialize(component);
            SetRefId(component, worldServices.GetNextRefID());
            return component;
        }

        public ISlot GetObjectRoot(bool explicitOnly = false)
        {
            if (GetComponent<ObjectRoot>() != null)
                return this;
            return Parent?.GetObjectRoot(explicitOnly);
        }

        public void ChildChanged(IWorldElement child) { }

        public void Destroy() { }

        public int ChildrenCount => children.Count;

        public ISlot this[int childIndex]
        {
            get
            {
                try
                {
                    return children[childIndex];
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public ISlot FindChild(Predicate<ISlot> filter, int maxDepth = -1)
        {
            ISlot found = children.Find(filter);
            if (found != null)
                return found;
            if (maxDepth == 0)
                return null;
            foreach (ISlot child in children)
            {
                found = child.FindChild(filter, maxDepth == -1 ? -1 : maxDepth - 1);
                if (found != null)
                    return found;
            }
            return null;
        }

        public ISlot FindChild(string name) => FindChild(s => s.Name == name);

        private static bool MatchSlot(ISlot slot, string name, bool matchSubstring, bool ignoreCase)
        {
            if (slot.Name == null)
            {
                return name == null;
            }

            if (name == null)
            {
                return false;
            }

            if (slot.Name == "")
            {
                return name == "";
            }

            StringComparison comparisonType = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (matchSubstring)
            {
                return slot.Name.IndexOf(name, comparisonType) >= 0;
            }

            return string.Compare(slot.Name, name, comparisonType) == 0;
        }

        public ISlot FindChild(
            string name,
            bool matchSubstring,
            bool ignoreCase,
            int maxDepth = -1
        ) => FindChild(s => MatchSlot(s, name, matchSubstring, ignoreCase), maxDepth);

        public T GetComponent<T>(Predicate<T> filter = null, bool excludeDisabled = false)
            where T : class
        {
            foreach (Component component in components)
            {
                if (
                    component is T t
                    && (!excludeDisabled || component.Enabled)
                    && (filter == null || filter(t))
                )
                    return t;
            }
            return null;
        }

        public Component GetComponent(Type type, bool exactTypeOnly = false)
        {
            foreach (Component component in components)
            {
                if (exactTypeOnly)
                {
                    if (component.GetType() == type)
                        return component;
                }
                else
                {
                    if (type.IsAssignableFrom(component.GetType()))
                        return component;
                }
            }
            return null;
        }

        public string GetSyncMemberName(ISyncMember member)
        {
            throw new NotImplementedException();
        }

        public void Load(DataTreeNode node, LoadControl control)
        {
            throw new NotImplementedException();
        }

        public DataTreeNode Save(SaveControl control)
        {
            throw new NotImplementedException();
        }
    }
}
