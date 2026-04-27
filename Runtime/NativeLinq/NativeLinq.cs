using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public interface IPredicate<T>
        where T : unmanaged
    {
        bool Match(in T value);
    }

    public interface ISelector<TSource, out TResult>
        where TSource : unmanaged
        where TResult : unmanaged
    {
        TResult Select(in TSource value);
    }

    public interface INativeAccumulator<T>
        where T : unmanaged
    {
        T Add(in T total, in T value);

        T Divide(in T total, int count);
    }

    public interface INativeEqualityComparer<T>
        where T : unmanaged
    {
        bool Equals(in T left, in T right);
    }

    public static class NativeLinq
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<T, TEnumerator> From<T, TEnumerator>(TEnumerator enumerator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return new Query<T, TEnumerator>(enumerator);
        }
    }

    public struct AscendingComparer<T> : IComparer<T>
        where T : unmanaged, IComparable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return x.CompareTo(y);
        }
    }

    public struct DescendingComparer<T> : IComparer<T>
        where T : unmanaged, IComparable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return y.CompareTo(x);
        }
    }

    public struct NativeEqualityComparer<T> : INativeEqualityComparer<T>
        where T : unmanaged, IEquatable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(in T left, in T right)
        {
            return left.Equals(right);
        }
    }
}
