using BovineLabs.Core.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static class UnsafeByteExtensions
    {
        public static unsafe void AddData<T>(this ref UnsafeList<byte> list, T data)
            where T : unmanaged
        {
            var oldLength = list.Length;
            list.Resize(list.Length + UnsafeUtility.SizeOf<T>());
            
            var ptr = list.Ptr + oldLength;
            UnsafeUtility.CopyStructureToPtr(ref data, ptr);
        }
        
        public static unsafe void AddDataUnsafe(this ref UnsafeList<byte> list, byte* data, int dataSize)
        {
            var oldLength = list.Length;
            list.Resize(list.Length + dataSize);
            
            var ptr = list.Ptr + oldLength;
            UnsafeUtility.MemCpy(ptr, data, dataSize);
        }
        
        public static unsafe void SetData<T>(this UnsafeArray<byte> arr, int index, T data)
            where T : unmanaged
        {
            var destPtr = (byte*)arr.GetUnsafePtr() + index * UnsafeUtility.SizeOf<T>();
            UnsafeUtility.CopyStructureToPtr(ref data, destPtr);
        }
        
        public static unsafe void SetDataUnsafe(this UnsafeArray<byte> array, int index, byte* data, int length)
        {
            var ptr = (byte*)array.GetUnsafePtr() + index * length;
            UnsafeUtility.MemCpy(ptr, data, length);
        }
        
        public static unsafe ref T GetDataAsRef<T>(this UnsafeArray<byte> array, int index)
            where T : unmanaged
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }
    }
}