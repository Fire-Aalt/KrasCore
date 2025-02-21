using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static class UnsafeUtilityExtra
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* AddressOf<T>(ref T output) where T : unmanaged
            => (T*)UnsafeUtility.AddressOf(ref output);
    }
}