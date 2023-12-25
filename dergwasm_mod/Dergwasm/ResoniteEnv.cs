using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Derg
{
    public class ResoniteEnv
    {
        public Machine machine;
        public Dictionary<ulong, IWorldElement> worldElements =
            new Dictionary<ulong, IWorldElement>();

        public ResoniteEnv(Machine machine)
        {
            this.machine = machine;
        }
    }
}
