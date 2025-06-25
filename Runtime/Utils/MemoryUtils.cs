using System;
using System.Runtime.InteropServices;

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
    }
}