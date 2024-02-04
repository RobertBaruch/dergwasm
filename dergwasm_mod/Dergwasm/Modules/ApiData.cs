using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Derg.Runtime;

namespace Derg.Modules
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
        public List<Parameter> Parameters { get; }
        public List<Parameter> Returns { get; }

        [JsonIgnore]
        public IEnumerable<ValueType> ParameterValueTypes => Parameters.Select(p => p.Type);

        [JsonIgnore]
        public IEnumerable<ValueType> ReturnValueTypes => Returns.Select(p => p.Type);

        public ApiFunc()
        {
            Parameters = new List<Parameter>();
            Returns = new List<Parameter>();
        }
    }
}
