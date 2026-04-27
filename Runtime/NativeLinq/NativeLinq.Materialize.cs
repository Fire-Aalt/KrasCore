using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            return NativeLinqUtilities.ToNativeList<T, TEnumerator>(GetEnumerator(), allocator);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> ToNativeList<T, TEnumerator>(TEnumerator enumerator, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            var list = new NativeList<T>(allocator);
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
            }

            enumerator.Dispose();
            return list;
        }
    }
}
