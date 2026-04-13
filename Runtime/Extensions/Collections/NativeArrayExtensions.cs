using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static class NativeArrayExtensions
    {
        public static NativeArray<byte> AsBytes<T>(this NativeArray<T> array)
            where T : unmanaged
        {
            return array.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
        }
    }
}