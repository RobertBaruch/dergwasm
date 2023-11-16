using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    public class Trap : Exception
    {
        public Trap() { }

        public Trap(string message)
            : base(message) { }
    }
}
