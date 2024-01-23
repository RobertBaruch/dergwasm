using System;
using System.Collections.Generic;
using Derg;
using Elements.Core;
using FrooxEngine;

namespace DergwasmTests
{
    public class TestWorldServices : IWorldServices
    {
        Dictionary<RefID, IWorldElement> objects = new Dictionary<RefID, IWorldElement>();

        public void AddRefID(IWorldElement obj, ulong i)
        {
            objects.Add(new RefID(i), obj);
        }

        public override IWorldElement GetObjectOrNull(RefID refID)
        {
            return objects.TryGetValue(refID, out IWorldElement obj) ? obj : null;
        }

        public override Slot GetRootSlot()
        {
            throw new NotImplementedException();
        }
    }
}
