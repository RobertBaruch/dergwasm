using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    public interface IModule
    {
        FuncType GetFuncType(uint funcidx);
    }
}
