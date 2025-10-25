using Unity.Collections;

namespace KrasCore
{
    public static class NativeSliceExtensions
    {
        public static NativeArray<T> ToNativeArray<T>(this NativeSlice<T> slice, Allocator allocator) where T : unmanaged
        {
            var arr = new NativeArray<T>(slice.Length, allocator);
            slice.CopyTo(arr);
            return arr;
        }
    }
}