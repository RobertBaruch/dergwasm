using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Dergwasm.Runtime;

namespace Dergwasm.Modules
{
    public class Parameter
    {
        public string Name { get; set; }
        public ValueType[] Types { get; set; }
        public string CSType { get; set; }
    }

    public class ApiFunc
    {
        public string Module { get; set; }
        public string Name { get; set; }
        public List<Parameter> Parameters { get; } = new List<Parameter>();
        public List<Parameter> Returns { get; } = new List<Parameter>();

        [JsonIgnore]
        public IEnumerable<ValueType> ParameterValueTypes => Parameters.SelectMany(p => p.Types);

        [JsonIgnore]
        public IEnumerable<ValueType> ReturnValueTypes => Returns.SelectMany(p => p.Types);
    }
}
