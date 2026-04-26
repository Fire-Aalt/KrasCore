using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Min<T, TEnumerator>(this NativeEnumerable<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.Min(new NativeAscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Max<T, TEnumerator>(this NativeEnumerable<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.Max(new NativeAscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Min<T, TEnumerator, TPredicate>(this NativeWhereEnumerable<T, TEnumerator, TPredicate> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            return source.Min(new NativeAscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Max<T, TEnumerator, TPredicate>(this NativeWhereEnumerable<T, TEnumerator, TPredicate> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            return source.Max(new NativeAscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult Min<TSource, TResult, TEnumerator, TSelector>(this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
        {
            return source.Min(new NativeAscendingComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult Max<TSource, TResult, TEnumerator, TSelector>(this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
        {
            return source.Max(new NativeAscendingComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult Min<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>(this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
        {
            return source.Min(new NativeAscendingComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult Max<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>(this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
        {
            return source.Max(new NativeAscendingComparer<TResult>());
        }
    }

    public partial struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Min<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.Min<T, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Max<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.Max<T, Enumerator, TComparer>(GetEnumerator(), comparer);
        }
    }

    public partial struct NativeWhereEnumerable<T, TEnumerator, TPredicate>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Min<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.Min<T, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Max<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.Max<T, Enumerator, TComparer>(GetEnumerator(), comparer);
        }
    }

    public partial struct NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Min<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.Min<TResult, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Max<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.Max<TResult, Enumerator, TComparer>(GetEnumerator(), comparer);
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
        public TResult Min<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.Min<TResult, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Max<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.Max<TResult, Enumerator, TComparer>(GetEnumerator(), comparer);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Min<T, TEnumerator, TComparer>(TEnumerator enumerator, TComparer comparer)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TComparer : unmanaged, IComparer<T>
        {
            if (!enumerator.MoveNext())
            {
                enumerator.Dispose();
                throw new InvalidOperationException("The NativeLinq source contains no elements.");
            }

            var best = enumerator.Current;
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                if (comparer.Compare(value, best) < 0)
                {
                    best = value;
                }
            }

            enumerator.Dispose();
            return best;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Max<T, TEnumerator, TComparer>(TEnumerator enumerator, TComparer comparer)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TComparer : unmanaged, IComparer<T>
        {
            if (!enumerator.MoveNext())
            {
                enumerator.Dispose();
                throw new InvalidOperationException("The NativeLinq source contains no elements.");
            }

            var best = enumerator.Current;
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                if (comparer.Compare(value, best) > 0)
                {
                    best = value;
                }
            }

            enumerator.Dispose();
            return best;
        }
    }
}
