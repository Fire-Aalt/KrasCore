using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Min<T, TEnumerator>(this Query<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.Min(new AscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Max<T, TEnumerator>(this Query<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.Max(new AscendingComparer<T>());
        }
    }

    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Min<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.Min<T, TEnumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Max<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.Max<T, TEnumerator, TComparer>(GetEnumerator(), comparer);
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
