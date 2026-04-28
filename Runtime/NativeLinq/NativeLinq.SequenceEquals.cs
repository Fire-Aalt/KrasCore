using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<T, TEnumerator, TOtherEnumerator>(
            this Query<T, TEnumerator> source,
            Query<T, TOtherEnumerator> other)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TOtherEnumerator : unmanaged, IEnumerator<T>
        {
            return source.SequenceEquals(other.GetEnumerator(), new NativeEqualityComparer<T>());
        }
    }

    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEquals<TOtherEnumerator, TEqualityComparer>(TOtherEnumerator other, TEqualityComparer comparer)
            where TOtherEnumerator : unmanaged, IEnumerator<T>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            var left = GetEnumerator();
            var right = other;
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
