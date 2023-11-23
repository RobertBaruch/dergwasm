using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    // The runtime representation of a module.
    public class ModuleInstance
    {
        public List<int> FuncTypesMap = new List<int>();
        public List<int> FuncsMap = new List<int>();
        public List<int> TablesMap = new List<int>();
        public List<int> ElementSegmentsMap = new List<int>();
        public List<int> GlobalsMap = new List<int>();
        public List<int> DataSegmentsMap = new List<int>();
        public Dictionary<string, Value> ExportsMap = new Dictionary<string, Value>();
    }
}
