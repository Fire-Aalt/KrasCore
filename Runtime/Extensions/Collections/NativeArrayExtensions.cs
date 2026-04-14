using BovineLabs.Core.Collections;
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
            var bytesView = array.AsBytes();
            var bytes = new UnsafeArray<byte>(array.Length * UnsafeUtility.SizeOf<T>(), allocator);
            
            UnsafeUtility.MemCpy(bytes.GetUnsafePtr(),
                bytesView.GetUnsafeReadOnlyPtr(), array.Length * UnsafeUtility.SizeOf<T>());
            return bytes;
        }
    }
}