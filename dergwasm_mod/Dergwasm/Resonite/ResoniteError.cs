using System;
using System.Runtime.CompilerServices;
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

    public static class CheckerExtensions
    {
        // Checks a Ptr<T> argument for null.
        public static void CheckNullArg<T>(this Ptr<T> ptr, string argname)
            where T : struct
        {
            if (ptr.IsNull)
            {
                throw new ResoniteException(ResoniteError.NullArgument, $"{argname} is null");
            }
        }

        public static void CheckNullArg<T>(this Output<T> output, string argname)
            where T : struct => output.Ptr.CheckNullArg(argname);

        // Checks a NullTerminatedString argument for null, or passes back the string
        // if not null.
        public static void CheckNullArg(
            this NullTerminatedString str,
            string argname,
            EmscriptenEnv env,
            out string result
        )
        {
            str.Data.CheckNullArg(argname);
            result = env.GetUTF8StringFromMem(str);
        }

        // Checks that a WasmRefID<T> is non-zero and is actually a T.
        public static void CheckValidRef<T>(
            this WasmRefID<T> refID,
            string argname,
            IWorldServices worldServices,
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

        // Returns an error, logging the exception.
        //
        // You should never pass a null exception in. This method is meant to be used
        // inside a catch block.
        public static ResoniteError ToError(this Exception e, [CallerMemberName] string method = "")
        {
            if (e is ResoniteException ex)
            {
                return ex.Error.LogError(e, method);
            }
            return ResoniteError.FailedPrecondition.LogError(e, method);
        }

        // Returns a specific error, logging the exception, or success if the exception
        // was null.
        //
        // You should never pass a null exception in. This method is meant to be used
        // inside a catch block.
        private static ResoniteError LogError(
            this ResoniteError err,
            Exception e = null,
            [CallerMemberName] string method = ""
        )
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
