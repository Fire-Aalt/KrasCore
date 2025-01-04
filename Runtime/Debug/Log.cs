using System;

namespace KrasCore.Runtime
{
    public static class Log
    {
        public static void LogCaller(
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0
            , [System.Runtime.CompilerServices.CallerMemberName] string memberName = ""
            , [System.Runtime.CompilerServices.CallerFilePath] string filePath = ""
        )
        {
            UnityEngine.Debug.Log($"{line} :: {memberName} :: {filePath}");
        }
        
        public static void ThrowCaller(string message,
            [System.Runtime.CompilerServices.CallerLineNumber] int line = 0
            , [System.Runtime.CompilerServices.CallerMemberName] string memberName = ""
            , [System.Runtime.CompilerServices.CallerFilePath] string filePath = ""
        )
        {
            throw new Exception($"{message}\n{line} :: {memberName} :: {filePath}");
        }
    }
}