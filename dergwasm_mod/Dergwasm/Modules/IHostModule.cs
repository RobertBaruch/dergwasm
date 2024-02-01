using System.Collections.Generic;
using System.Linq;

namespace Derg.Modules
{
    public interface IHostModule
    {
        List<HostFunc> Functions { get; }
        List<ApiFunc> ApiData { get; }
    }

    public static class HostModuleExtensions {
        public static HostFunc GetHostFunc(this IHostModule module, string name) {
            return module.Functions.First(f => f.Name == name);
        }

        public static ApiFunc GetApiFunc(this IHostModule module, string name) {
            return module.ApiData.First(f => f.Name == name);
        }
    }
}
