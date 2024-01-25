using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;

namespace Derg
{
    public class WorldServices : IWorldServices
    {
        World world;

        public WorldServices(World world)
        {
            this.world = world;
        }

        public override IWorldElement GetObjectOrNull(RefID refID)
        {
            return world.ReferenceController.GetObjectOrNull(refID);
        }

        public override Slot GetRootSlot()
        {
            return world.RootSlot;
        }
    }
}
