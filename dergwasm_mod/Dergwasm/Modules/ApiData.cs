using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Derg.Modules
{
    public class Parameter
    {
        public string Name { get; set; }
        public ValueType Type { get; set; }
        public string CSType { get; set; }
    }

    public class Api
    {
        public string Module { get; set; }
        public string Name { get; set; }
        public List<Parameter> Parameters { get; }
        public List<Parameter> Returns { get; }

        [JsonIgnore]
        public List<ValueType> ParameterValueTypes { get; }

        [JsonIgnore]
        public List<ValueType> ReturnValueTypes { get; }

        public Api()
        {
            Parameters = new List<Parameter>();
            Returns = new List<Parameter>();
            ParameterValueTypes = new List<ValueType>();
            ReturnValueTypes = new List<ValueType>();
        }
    }
}
