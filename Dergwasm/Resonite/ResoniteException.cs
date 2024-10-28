using System;

namespace Dergwasm.Resonite
{
    public class ResoniteException : Exception
    {
        public ResoniteError Error { get; }

        public ResoniteException(ResoniteError error)
        {
            Error = error;
        }

        public ResoniteException(ResoniteError error, string msg)
            : base(msg)
        {
            Error = error;
        }
    }
}
