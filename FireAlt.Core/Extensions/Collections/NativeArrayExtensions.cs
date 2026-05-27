using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FireAlt.Core.Extensions
{
    public static class NativeArrayExtensions
    {
        public static NativeArray<byte> AsBytes<T>(this NativeArray<T> array)
            where T : unmanaged
        {
            return array.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
        }
        
        public static NativeArray<byte> ToBytes<T>(this NativeArray<T> array, Allocator allocator)
            where T : unmanaged
        {
            var bytesView = array.AsBytes();
            var bytes = new NativeArray<byte>(array.Length * UnsafeUtility.SizeOf<T>(), allocator);
            bytes.CopyFrom(bytesView);
            return bytes;
        }
        
        public static unsafe UnsafeArray<byte> ToBytesUnsafe<T>(this NativeArray<T> array, Allocator allocator)
            where T : unmanaged
        {
            var bytes = new UnsafeArray<byte>(array.Length * UnsafeUtility.SizeOf<T>(), allocator);
            UnsafeUtility.MemCpy(bytes.GetUnsafePtr(), array.GetUnsafeReadOnlyPtr(), array.Length * UnsafeUtility.SizeOf<T>());
            return bytes;
        }
    }
}