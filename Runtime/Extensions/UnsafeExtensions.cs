using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static class UnsafeExtensions
    {
        public static unsafe T* GetTempPtr<T>(this T[] source) where T : unmanaged
        {
            var dest = new NativeArray<T>(source.Length, Allocator.Temp);
            
            fixed (T* array = source)
            {
                var byteLength = source.Length * UnsafeUtility.SizeOf<T>();
                UnsafeUtility.MemCpy(dest.GetUnsafePtr(), array, byteLength);
            }

            return (T*)dest.GetUnsafePtr();
        }
        
        public static unsafe void CopyToUnsafe<T1, T2>(this NativeList<T2> source, T1[] destination, int count) where T1 : unmanaged where T2 : unmanaged
        {
            if (UnsafeUtility.SizeOf<T1>() != UnsafeUtility.SizeOf<T2>()) throw new Exception("Stride size is not the same");
            
            if (source.Length < count) throw new Exception("Source is smaller than count");
            
            if (destination.Length < count) throw new Exception("Destination is smaller than count");
            
            CopyToUnsafe(ref destination[0], source.GetUnsafeReadOnlyPtr(), count);
        }
        
        public static unsafe void CopyToUnsafe<T1, T2>(this NativeArray<T2> source, T1[] destination, int count) where T1 : unmanaged where T2 : unmanaged
        {
            if (UnsafeUtility.SizeOf<T1>() != UnsafeUtility.SizeOf<T2>()) throw new Exception("Stride size is not the same");
            
            if (source.Length < count) throw new Exception("Source is smaller than count");
            
            if (destination.Length < count) throw new Exception("Destination is smaller than count");
            
            CopyToUnsafe(ref destination[0], source.GetUnsafeReadOnlyPtr(), count);
        }
        
        public static unsafe void CopyToUnsafe<T1, T2>(this NativeArray<T2> source, NativeArray<T1> destination,
            int count, int sourceOffset, int destinationOffset) where T1 : unmanaged where T2 : unmanaged
        {
            if (UnsafeUtility.SizeOf<T1>() != UnsafeUtility.SizeOf<T2>()) throw new Exception("Stride size is not the same");
            
            if (source.Length < sourceOffset + count) throw new Exception("Source is smaller than count");
            
            if (destination.Length < destinationOffset + count) throw new Exception("Destination is smaller than count");
            
            CopyToUnsafe<T1>(destination.GetUnsafePtr(), source.GetUnsafeReadOnlyPtr(), count, sourceOffset, destinationOffset);
        }
        
        private static unsafe void CopyToUnsafe<T>(ref T destination, void* source, int count) where T : unmanaged
        {
            var byteLength = count * UnsafeUtility.SizeOf<T>();
            var managedBuffer = UnsafeUtility.AddressOf(ref destination);
            UnsafeUtility.MemCpy(managedBuffer, source, byteLength);
        }
        
        public static unsafe void CopyToUnsafe<T>(void* destination, void* source, int count,
            int sourceOffset, int destinationOffset) where T : unmanaged
        {
            var copySizeInBytes = count * UnsafeUtility.SizeOf<T>();
                       
            UnsafeUtility.MemCpy(
                (byte*)destination + destinationOffset * UnsafeUtility.SizeOf<T>(),
                (byte*)source + sourceOffset * UnsafeUtility.SizeOf<T>(),
                copySizeInBytes);
        }
    }
}