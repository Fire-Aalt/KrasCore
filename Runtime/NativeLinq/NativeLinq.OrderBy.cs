using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrderedQuery<T, TEnumerator, AscendingComparer<T>> OrderBy<T, TEnumerator>(this Query<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new AscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrderedQuery<T, TEnumerator, DescendingComparer<T>> OrderByDescending<T, TEnumerator>(this Query<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new DescendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> ToOrderedBy<T, TEnumerator>(this Query<T, TEnumerator> source, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.ToOrderedBy(new AscendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> ToOrderedByDescending<T, TEnumerator>(this Query<T, TEnumerator> source, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.ToOrderedBy(new DescendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrderedQuery<T, TEnumerator, ThenByComparer<T, TComparer, AscendingComparer<T>>> ThenBy<T, TEnumerator, TComparer>(
            this OrderedQuery<T, TEnumerator, TComparer> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TComparer : unmanaged, IComparer<T>
        {
            return source.ThenBy(new AscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrderedQuery<T, TEnumerator, ThenByComparer<T, TComparer, DescendingComparer<T>>> ThenByDescending<T, TEnumerator, TComparer>(
            this OrderedQuery<T, TEnumerator, TComparer> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TComparer : unmanaged, IComparer<T>
        {
            return source.ThenBy(new DescendingComparer<T>());
        }
    }

    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrderedQuery<T, TEnumerator, TComparer> OrderBy<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return new OrderedQuery<T, TEnumerator, TComparer>(this, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrderedQuery<T, TEnumerator, ReverseComparer<T, TComparer>> OrderByDescending<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return new OrderedQuery<T, TEnumerator, ReverseComparer<T, TComparer>>(this, new ReverseComparer<T, TComparer>(comparer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToOrderedBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<T>
        {
            var list = ToNativeList(allocator);
            list.Sort(comparer);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToOrderedByDescending<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<T>
        {
            var list = ToNativeList(allocator);
            list.Sort(new ReverseComparer<T, TComparer>(comparer));
            return list;
        }
    }

    public struct ThenByComparer<T, TPrimaryComparer, TSecondaryComparer> : IComparer<T>
        where T : unmanaged
        where TPrimaryComparer : unmanaged, IComparer<T>
        where TSecondaryComparer : unmanaged, IComparer<T>
    {
        private TPrimaryComparer _primaryComparer;
        private TSecondaryComparer _secondaryComparer;

        public ThenByComparer(TPrimaryComparer primaryComparer, TSecondaryComparer secondaryComparer)
        {
            _primaryComparer = primaryComparer;
            _secondaryComparer = secondaryComparer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            var result = _primaryComparer.Compare(x, y);
            return result != 0 ? result : _secondaryComparer.Compare(x, y);
        }
    }

    public struct ReverseComparer<T, TComparer> : IComparer<T>
        where T : unmanaged
        where TComparer : unmanaged, IComparer<T>
    {
        private TComparer _comparer;

        public ReverseComparer(TComparer comparer)
        {
            _comparer = comparer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return _comparer.Compare(y, x);
        }
    }
}
