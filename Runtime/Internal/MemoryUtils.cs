using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static class MemoryUtils
    {
        public static object ByteArrayToStructure(byte[] bytes, Type type)
        {
            var ptr = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                return Marshal.PtrToStructure(ptr, type);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public static void StructureToByteArray(object obj, byte[] bytes)
        {
            var ptr = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.StructureToPtr(obj, ptr, false);
                Marshal.Copy(ptr, bytes, 0, bytes.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        
        public static unsafe NativeArray<byte> StructureToNativeByteArray(object obj, Allocator allocator)
        {
            var size = Marshal.SizeOf(obj);
            var dest = new NativeArray<byte>(size, allocator);
            
            Marshal.StructureToPtr(obj, (IntPtr)dest.GetUnsafePtr(), false);
            return dest;
        }
    }
}