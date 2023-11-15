using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    public class Func { }

    public class ModuleFunc : Func
    {
        public FuncType signature;
        public ValueType[] locals;
        public List<Instruction> code;
    }
}
