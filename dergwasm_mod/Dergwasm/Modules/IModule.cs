using System;

namespace Derg.Modules
{
    public interface IModule
    {
        Memory<Func> Functions { get; }
    }
}
