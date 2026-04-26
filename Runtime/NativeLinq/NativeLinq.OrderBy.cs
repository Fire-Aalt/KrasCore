using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderBy<T, TEnumerator>(this NativeQuery<T, TEnumerator> source, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new NativeAscendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderByDescending<T, TEnumerator>(this NativeQuery<T, TEnumerator> source, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new NativeDescendingComparer<T>(), allocator);
        }
    }

    public partial struct NativeQuery<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.OrderBy<T, TEnumerator, TComparer>(GetEnumerator(), comparer, allocator);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderBy<T, TEnumerator, TComparer>(
            TEnumerator enumerator,
            TComparer comparer,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TComparer : unmanaged, IComparer<T>
        {
            var list = ToNativeList<T, TEnumerator>(enumerator, allocator);
            list.Sort(comparer);
            return list;
        }
    }
}
