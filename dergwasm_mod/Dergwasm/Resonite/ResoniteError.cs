using System;
using Derg.Wasm;

namespace Derg.Resonite
{
    // Error codes for API return values.
    public enum ResoniteError : int
    {
        Success = 0,
        NullArgument = -1,
        InvalidRefId = -2,
        FailedPrecondition = -3,
    }

    // A mixin class for error checking.
    public class ErrorChecker
    {
        // Checks a Ptr<T> argument for null.
        public void CheckNullArg<T>(string method, string argname, Ptr<T> ptr)
            where T : struct
        {
            if (ptr.IsNull)
            {
                throw new ResoniteException(
                    ResoniteError.NullArgument,
                    $"{method}: {argname} is null"
                );
            }
        }

        // Checks a NullTerminatedString argument for null, or passes back the string
        // if not null.
        public void CheckNullArg(
            string method,
            string argname,
            EmscriptenEnv env,
            NullTerminatedString str,
            out string result
        )
        {
            CheckNullArg(method, argname, str.Data);
            result = env.GetUTF8StringFromMem(str);
        }

        // Checks that a WasmRefID<T> is non-zero and is actually a T.
        public void CheckValidRef<T>(
            string method,
            string argname,
            IWorldServices worldServices,
            WasmRefID<T> refID,
            out T instance
        )
            where T : class, FrooxEngine.IWorldElement
        {
            instance = worldServices.GetObjectOrNull(refID);
            if (instance == null)
            {
                throw new ResoniteException(
                    ResoniteError.InvalidRefId,
                    $"{argname} is not a valid reference to a {typeof(T)}"
                );
            }
        }

        // Returns a FailedPreconditionError, logging the exception.
        //
        // You should never pass a null exception in. This method is meant to be used
        // inside a catch block.
        public ResoniteError ReturnError(string method, Exception e) =>
            ReturnError(method, ResoniteError.FailedPrecondition, e);

        // Returns a specific error, logging the exception, or success if the exception
        // was null.
        //
        // You should never pass a null exception in. This method is meant to be used
        // inside a catch block.
        public ResoniteError ReturnError(string method, ResoniteError err, Exception e)
        {
            if (e != null)
            {
                DergwasmMachine.Msg($"[Dergwasm] Exception in {method}: {e}");
                return err;
            }
            return ResoniteError.Success;
        }
    }
}
