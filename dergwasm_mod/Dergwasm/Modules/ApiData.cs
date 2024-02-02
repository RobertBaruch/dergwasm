﻿using System.Collections.Generic;
using System.Text.Json.Serialization;
using Derg.Runtime;

namespace Derg.Modules
{
    public class Parameter
    {
        public string Name { get; set; }
        public ValueType Type { get; set; }
        public string CSType { get; set; }
    }

    public class ApiFunc
    {
        public string Module { get; set; }
        public string Name { get; set; }
        public List<Parameter> Parameters { get; }
        public List<Parameter> Returns { get; }

        [JsonIgnore]
        public List<ValueType> ParameterValueTypes { get; }

        [JsonIgnore]
        public List<ValueType> ReturnValueTypes { get; }

        public ApiFunc()
        {
            Parameters = new List<Parameter>();
            Returns = new List<Parameter>();
            ParameterValueTypes = new List<ValueType>();
            ReturnValueTypes = new List<ValueType>();
        }
    }
}
