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

    public class ExitTrap : Trap
    {
        public int ExitCode;

        public ExitTrap(int exitCode)
            : base($"Exit trap: {exitCode}")
        {
            ExitCode = exitCode;
        }
    }
}
