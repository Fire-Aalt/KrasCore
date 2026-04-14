using System;
using System.Diagnostics;
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
        
        
        public static unsafe void CopyFrom<T>(this UnsafeArray<T> dst, NativeArray<T> src)
            where T : unmanaged
        {
            UnsafeUtility.MemCpy(dst.GetUnsafePtr(),
                src.GetUnsafeReadOnlyPtr(), dst.Length * UnsafeUtility.SizeOf<T>());
        }
        
        private static unsafe UnsafeArray<U> InternalReinterpret<T, U>(this UnsafeArray<T> array, int length)
            where T : unmanaged
            where U : unmanaged
        {
            UnsafeArray<U> nativeArray = UnsafeArrayUnsafeUtility.ConvertExistingDataToUnsafeArray<U>(array.buffer, length, array.allocatorLabel);
            return nativeArray;
        }
        
        public static UnsafeArray<U> Reinterpret<T, U>(this UnsafeArray<T> array, int expectedTypeSize)
            where T : unmanaged
            where U : unmanaged
        {
            long tSize = UnsafeUtility.SizeOf<T>();
            long uSize = UnsafeUtility.SizeOf<U>();
            long byteLen = array.Length * tSize;
            long num = byteLen / uSize;
            array.CheckReinterpretSize<T, U>(tSize, uSize, expectedTypeSize, byteLen, num);
            return array.InternalReinterpret<T, U>((int) num);
        }

        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReinterpretSize<T, U>(this UnsafeArray<T> array)
            where T : unmanaged
            where U : unmanaged
        {
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<U>())
                throw new InvalidOperationException($"Types {typeof (T)} and {typeof (U)} are different sizes - direct reinterpretation is not possible. If this is what you intended, use Reinterpret(<type size>)");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReinterpretSize<T, U>(this UnsafeArray<T> array,
            long tSize,
            long uSize,
            int expectedTypeSize,
            long byteLen,
            long uLen)
            where T : unmanaged
            where U : unmanaged
        {
            if (tSize != (long) expectedTypeSize)
                throw new InvalidOperationException($"Type {typeof (T)} was expected to be {expectedTypeSize} but is {tSize} bytes");
            if (uLen * uSize != byteLen)
                throw new InvalidOperationException($"Types {typeof (T)} (array length {array.Length}) and {typeof (U)} cannot be aliased due to size constraints. The size of the types and lengths involved must line up.");
        }
    }

    public static class UnsafeArrayUnsafeUtility
    {
        public static unsafe UnsafeArray<T> ConvertExistingDataToUnsafeArray<T>(
            void* dataPointer,
            int length,
            Allocator allocator)
            where T : unmanaged
        {
            CheckConvertArguments(length);
            return new UnsafeArray<T>
            {
                buffer = dataPointer,
                allocatorLabel = allocator,
                length = length
            };
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckConvertArguments(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof (length), "Length must be >= 0");
        }
    }
}