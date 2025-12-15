#if BL_CORE
using BovineLabs.Core.Collections;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static class UnsafeArrayExtensions
    {
        public static string ToArrayString<T>(this UnsafeArray<T> list)
            where T : unmanaged
        {
            var s = "";
            foreach (var value in list)
            {
                s += value + ", ";
            }
            return s;
        }
        
        public static unsafe NativeArray<T> AsNativeArray<T>(this UnsafeArray<T> unsafeArray)
            where T : unmanaged
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(unsafeArray.GetUnsafePtr(), unsafeArray.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            return array;
        }
        
        public static unsafe ref T ElementAt<T>(this UnsafeArray<T> unsafeArray, int index)
            where T : unmanaged
        {
            Assert.IsTrue(index >= 0 && index < unsafeArray.Length);
            return ref UnsafeUtility.ArrayElementAsRef<T>(unsafeArray.GetUnsafePtr(), index);
        }
        
        public static unsafe void Clear<T>(this UnsafeArray<T> array)
            where T : unmanaged
        {
            UnsafeUtility.MemClear(array.GetUnsafePtr(), UnsafeUtility.SizeOf<T>() * array.Length);
        }
        
        public static unsafe void Set<T>(this UnsafeArray<T> array, T value)
            where T : unmanaged
        {
            UnsafeUtility.MemCpyReplicate(array.GetUnsafePtr(), UnsafeUtility.AddressOf(ref value),
                UnsafeUtility.SizeOf<T>(), array.Length);
        }
    }
}
#endif