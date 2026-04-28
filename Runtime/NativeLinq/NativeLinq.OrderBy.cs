using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<T, NativeArray<T>.Enumerator> OrderBy<T, TEnumerator>(this Query<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new AscendingComparer<T>(), Allocator.Temp).AsQuery();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<T, NativeArray<T>.Enumerator> OrderByDescending<T, TEnumerator>(this Query<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new DescendingComparer<T>(), Allocator.Temp).AsQuery();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderBy<T, TEnumerator>(this Query<T, TEnumerator> source, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new AscendingComparer<T>(), allocator);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderByDescending<T, TEnumerator>(this Query<T, TEnumerator> source, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new DescendingComparer<T>(), allocator);
        }
    }

    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<T>
        {
            var list = ToNativeList(allocator);
            list.Sort(comparer);
            return list;
        }
    }
}
