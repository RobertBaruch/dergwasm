using Elements.Core;
using FrooxEngine;

namespace Derg
{
    // Interface for calls involving World, for testability.
    public abstract class IWorldServices
    {
        public abstract Slot GetRootSlot();

        public abstract IWorldElement GetObjectOrNull(RefID refID);
    }
}
