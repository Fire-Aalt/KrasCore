using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<T, TEnumerator, TOtherEnumerator>(
            this NativeQuery<T, TEnumerator> source,
            NativeQuery<T, TOtherEnumerator> other)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TOtherEnumerator : unmanaged, IEnumerator<T>
        {
            return source.SequenceEquals(other.GetEnumerator(), new NativeEqualityComparer<T>());
        }
    }

    public partial struct NativeQuery<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEquals<TOtherEnumerator, TEqualityComparer>(TOtherEnumerator other, TEqualityComparer comparer)
            where TOtherEnumerator : unmanaged, IEnumerator<T>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            return NativeLinqUtilities.SequenceEquals<T, TEnumerator, TOtherEnumerator, TEqualityComparer>(
                GetEnumerator(),
                other,
                comparer);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<T, TLeftEnumerator, TRightEnumerator, TEqualityComparer>(
            TLeftEnumerator left,
            TRightEnumerator right,
            TEqualityComparer comparer)
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
