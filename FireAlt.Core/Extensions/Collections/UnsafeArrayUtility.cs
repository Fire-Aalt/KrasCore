using System;
using System.Diagnostics;
using Unity.Collections;

namespace KrasCore
{
    public static class UnsafeArrayUtility
    {
        public static unsafe UnsafeArray<T> ConvertExistingDataToUnsafeArray<T>(void* dataPointer, int length, Allocator allocator)
            where T : unmanaged
        {
            CheckConvertArguments(length);
            return new UnsafeArray<T>
            {
                buffer = dataPointer,
                allocatorLabel = allocator,
                length = length
            };
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckConvertArguments(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof (length), "Length must be >= 0");
        }
    }
}