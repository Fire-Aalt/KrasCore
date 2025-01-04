using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static class UnsafeExtensions
    {
        public static unsafe void CopyToUnsafe<T1, T2>(this NativeList<T2> source, T1[] destination, int count) where T1 : unmanaged where T2 : unmanaged
        {
            if (UnsafeUtility.SizeOf<T1>() != UnsafeUtility.SizeOf<T2>()) throw new Exception("Stride size is not the same");
            
            if (source.Length < count) throw new Exception("Source is smaller than count");
            
            if (destination.Length < count) throw new Exception("Destination is smaller than count");
            
            CopyToUnsafe(ref destination[0], source.GetUnsafePtr(), count);
        }
        
        public static unsafe void CopyToUnsafe<T1, T2>(this NativeArray<T2> source, T1[] destination, int count) where T1 : unmanaged where T2 : unmanaged
        {
            if (UnsafeUtility.SizeOf<T1>() != UnsafeUtility.SizeOf<T2>()) throw new Exception("Stride size is not the same");
            
            if (source.Length < count) throw new Exception("Source is smaller than count");
            
            if (destination.Length < count) throw new Exception("Destination is smaller than count");
            
            CopyToUnsafe(ref destination[0], source.GetUnsafePtr(), count);
        }
        
        private static unsafe void CopyToUnsafe<T>(ref T destination, void* source, int count) where T : unmanaged
        {
            var byteLength = count * UnsafeUtility.SizeOf<T>();
            var managedBuffer = UnsafeUtility.AddressOf(ref destination);
            UnsafeUtility.MemCpy(managedBuffer, source, byteLength);
        }
    }
}