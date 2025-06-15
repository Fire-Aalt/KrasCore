using BovineLabs.Core.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static class UnsafeArrayExtensions
    {
        public static unsafe NativeArray<T> AsNativeArray<T>(this UnsafeArray<T> unsafeArray)
            where T : unmanaged
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(unsafeArray.GetUnsafePtr(), unsafeArray.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            return array;
        }
    }
}