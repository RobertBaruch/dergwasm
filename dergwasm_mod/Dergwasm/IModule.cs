using Derg.Runtime;

namespace Derg
{
    public interface IModule
    {
        FuncType GetFuncType(int funcidx);
    }
}
