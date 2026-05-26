using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace KrasCore
{
    public static class UnsafeListExtensions
    {
        public static unsafe NativeArray<T> AsNativeArray<T>(this UnsafeList<T> unsafeList)
            where T : unmanaged
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(unsafeList.Ptr, unsafeList.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            return array;
        }
        
        public static void EnsureCapacity<T>(this ref UnsafeList<T> list, int minCapacity) where T : unmanaged
        {
            var newCapacity = math.max(list.Capacity, minCapacity);

            while (list.Capacity < newCapacity)
            {
                list.Capacity *= 2;
            }
        }
    }
}