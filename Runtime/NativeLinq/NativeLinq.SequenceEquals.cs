using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<T, TEnumerator, TOtherEnumerator>(this NativeEnumerable<T, TEnumerator> source, NativeEnumerable<T, TOtherEnumerator> other)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TOtherEnumerator : unmanaged, IEnumerator<T>
        {
            return source.SequenceEquals(other.GetEnumerator(), new NativeEqualityComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<T, TEnumerator, TPredicate, TOtherEnumerator>(this NativeWhereEnumerable<T, TEnumerator, TPredicate> source, NativeEnumerable<T, TOtherEnumerator> other)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
            where TOtherEnumerator : unmanaged, IEnumerator<T>
        {
            return source.SequenceEquals(other.GetEnumerator(), new NativeEqualityComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<TSource, TResult, TEnumerator, TSelector, TOtherEnumerator>(this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source, NativeEnumerable<TResult, TOtherEnumerator> other)
            where TSource : unmanaged
            where TResult : unmanaged, IEquatable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
            where TOtherEnumerator : unmanaged, IEnumerator<TResult>
        {
            return source.SequenceEquals(other.GetEnumerator(), new NativeEqualityComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector, TOtherEnumerator>(this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source, NativeEnumerable<TResult, TOtherEnumerator> other)
            where TSource : unmanaged
            where TResult : unmanaged, IEquatable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
            where TOtherEnumerator : unmanaged, IEnumerator<TResult>
        {
            return source.SequenceEquals(other.GetEnumerator(), new NativeEqualityComparer<TResult>());
        }
    }

    public partial struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEquals<TOtherEnumerator, TEqualityComparer>(TOtherEnumerator other, TEqualityComparer comparer)
            where TOtherEnumerator : unmanaged, IEnumerator<T>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            return NativeLinqUtilities.SequenceEquals<T, Enumerator, TOtherEnumerator, TEqualityComparer>(GetEnumerator(), other, comparer);
        }
    }

    public partial struct NativeWhereEnumerable<T, TEnumerator, TPredicate>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEquals<TOtherEnumerator, TEqualityComparer>(TOtherEnumerator other, TEqualityComparer comparer)
            where TOtherEnumerator : unmanaged, IEnumerator<T>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            return NativeLinqUtilities.SequenceEquals<T, Enumerator, TOtherEnumerator, TEqualityComparer>(GetEnumerator(), other, comparer);
        }
    }

    public partial struct NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEquals<TOtherEnumerator, TEqualityComparer>(TOtherEnumerator other, TEqualityComparer comparer)
            where TOtherEnumerator : unmanaged, IEnumerator<TResult>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<TResult>
        {
            return NativeLinqUtilities.SequenceEquals<TResult, Enumerator, TOtherEnumerator, TEqualityComparer>(GetEnumerator(), other, comparer);
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
        public bool SequenceEquals<TOtherEnumerator, TEqualityComparer>(TOtherEnumerator other, TEqualityComparer comparer)
            where TOtherEnumerator : unmanaged, IEnumerator<TResult>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<TResult>
        {
            return NativeLinqUtilities.SequenceEquals<TResult, Enumerator, TOtherEnumerator, TEqualityComparer>(GetEnumerator(), other, comparer);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<T, TLeftEnumerator, TRightEnumerator, TEqualityComparer>(TLeftEnumerator left, TRightEnumerator right, TEqualityComparer comparer)
            where T : unmanaged
            where TLeftEnumerator : unmanaged, IEnumerator<T>
            where TRightEnumerator : unmanaged, IEnumerator<T>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            while (true)
            {
                var leftHasValue = left.MoveNext();
                var rightHasValue = right.MoveNext();
                if (leftHasValue != rightHasValue)
                {
                    left.Dispose();
                    right.Dispose();
                    return false;
                }

                if (!leftHasValue)
                {
                    left.Dispose();
                    right.Dispose();
                    return true;
                }

                var leftValue = left.Current;
                var rightValue = right.Current;
                if (!comparer.Equals(in leftValue, in rightValue))
                {
                    left.Dispose();
                    right.Dispose();
                    return false;
                }
            }
        }
    }
}
