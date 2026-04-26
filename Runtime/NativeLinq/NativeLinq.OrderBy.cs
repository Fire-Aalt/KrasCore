using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderBy<T, TEnumerator>(
            this NativeEnumerable<T, TEnumerator> source,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new NativeAscendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderByDescending<T, TEnumerator>(
            this NativeEnumerable<T, TEnumerator> source,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new NativeDescendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderBy<T, TEnumerator, TPredicate>(
            this NativeWhereEnumerable<T, TEnumerator, TPredicate> source,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            return source.OrderBy(new NativeAscendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderByDescending<T, TEnumerator, TPredicate>(
            this NativeWhereEnumerable<T, TEnumerator, TPredicate> source,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            return source.OrderBy(new NativeDescendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<TResult> OrderBy<TSource, TResult, TEnumerator, TSelector>(
            this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source,
            AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
        {
            return source.OrderBy(new NativeAscendingComparer<TResult>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<TResult> OrderByDescending<TSource, TResult, TEnumerator, TSelector>(
            this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source,
            AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
        {
            return source.OrderBy(new NativeDescendingComparer<TResult>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<TResult> OrderBy<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>(
            this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source,
            AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
        {
            return source.OrderBy(new NativeAscendingComparer<TResult>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<TResult> OrderByDescending<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>(
            this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source,
            AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
        {
            return source.OrderBy(new NativeDescendingComparer<TResult>(), allocator);
        }
    }

    public partial struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.OrderBy<T, Enumerator, TComparer>(GetEnumerator(), comparer, allocator);
        }
    }

    public partial struct NativeWhereEnumerable<T, TEnumerator, TPredicate>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.OrderBy<T, Enumerator, TComparer>(GetEnumerator(), comparer, allocator);
        }
    }

    public partial struct NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<TResult> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.OrderBy<TResult, Enumerator, TComparer>(GetEnumerator(), comparer, allocator);
        }
    }

    public partial struct NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TSourceEnumerator : unmanaged, IEnumerator<TSource>
        where TInnerEnumerator : unmanaged, IEnumerator<TResult>
        where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<TResult> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.OrderBy<TResult, Enumerator, TComparer>(GetEnumerator(), comparer, allocator);
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


