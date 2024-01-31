using System.Collections.Generic;

namespace Derg.Modules
{
    public interface IHostModule
    {
        List<HostFunc> Functions { get; }
        List<ApiFunc> ApiData { get; }
    }
}
