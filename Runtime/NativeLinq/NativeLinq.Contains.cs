using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T, TEnumerator>(this NativeEnumerable<T, TEnumerator> source, T value)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.Contains(value, new NativeEqualityComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T, TEnumerator, TPredicate>(this NativeWhereEnumerable<T, TEnumerator, TPredicate> source, T value)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            return source.Contains(value, new NativeEqualityComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<TSource, TResult, TEnumerator, TSelector>(this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source, TResult value)
            where TSource : unmanaged
            where TResult : unmanaged, IEquatable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
        {
            return source.Contains(value, new NativeEqualityComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>(this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source, TResult value)
            where TSource : unmanaged
            where TResult : unmanaged, IEquatable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
        {
            return source.Contains(value, new NativeEqualityComparer<TResult>());
        }
    }

    public partial struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TEqualityComparer>(T value, TEqualityComparer comparer)
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            return NativeLinqUtilities.Contains<T, Enumerator, TEqualityComparer>(GetEnumerator(), value, comparer);
        }
    }

    public partial struct NativeWhereEnumerable<T, TEnumerator, TPredicate>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TEqualityComparer>(T value, TEqualityComparer comparer)
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            return NativeLinqUtilities.Contains<T, Enumerator, TEqualityComparer>(GetEnumerator(), value, comparer);
        }
    }

    public partial struct NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TEqualityComparer>(TResult value, TEqualityComparer comparer)
            where TEqualityComparer : unmanaged, INativeEqualityComparer<TResult>
        {
            return NativeLinqUtilities.Contains<TResult, Enumerator, TEqualityComparer>(GetEnumerator(), value, comparer);
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
        public bool Contains<TEqualityComparer>(TResult value, TEqualityComparer comparer)
            where TEqualityComparer : unmanaged, INativeEqualityComparer<TResult>
        {
            return NativeLinqUtilities.Contains<TResult, Enumerator, TEqualityComparer>(GetEnumerator(), value, comparer);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T, TEnumerator, TEqualityComparer>(TEnumerator enumerator, T value, TEqualityComparer comparer)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (comparer.Equals(in current, in value))
                {
                    enumerator.Dispose();
                    return true;
                }
            }

            enumerator.Dispose();
            return false;
        }
    }
}
