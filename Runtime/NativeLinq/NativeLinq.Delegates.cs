using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public delegate bool NativeLinqPredicate<T>(in T value)
        where T : unmanaged;

    public delegate TResult NativeLinqSelector<TSource, TResult>(in TSource value)
        where TSource : unmanaged
        where TResult : unmanaged;

    public readonly struct NativeDelegateQuery<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
    }

    public struct NativeDelegateWhereQuery<T, TEnumerator> : IEnumerator<T>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        public T Current => Throw<T>();

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            return Throw<bool>();
        }

        public void Reset()
        {
            Throw();
        }

        public void Dispose()
        {
        }

        private static void Throw()
        {
            throw new InvalidOperationException("NativeLinq delegate query was not IL-woven.");
        }

        private static TResult Throw<TResult>()
        {
            throw new InvalidOperationException("NativeLinq delegate query was not IL-woven.");
        }
    }

    public struct NativeDelegateSelectQuery<TSource, TResult, TEnumerator> : IEnumerator<TResult>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
    {
        public TResult Current => Throw<TResult>();

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            return Throw<bool>();
        }

        public void Reset()
        {
            Throw();
        }

        public void Dispose()
        {
        }

        private static void Throw()
        {
            throw new InvalidOperationException("NativeLinq delegate query was not IL-woven.");
        }

        private static T Throw<T>()
        {
            throw new InvalidOperationException("NativeLinq delegate query was not IL-woven.");
        }
    }

    public static class NativeLinqDelegateExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static NativeDelegateQuery<T, TEnumerator> WithDelegates<T, TEnumerator>(this Query<T, TEnumerator> source)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return Throw<NativeDelegateQuery<T, TEnumerator>>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static NativeDelegateQuery<T, NativeDelegateWhereQuery<T, TEnumerator>> Where<T, TEnumerator>(
            this NativeDelegateQuery<T, TEnumerator> source,
            NativeLinqPredicate<T> predicate)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return Throw<NativeDelegateQuery<T, NativeDelegateWhereQuery<T, TEnumerator>>>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static NativeDelegateQuery<TResult, NativeDelegateSelectQuery<TSource, TResult, TEnumerator>> Select<TSource, TResult, TEnumerator>(
            this NativeDelegateQuery<TSource, TEnumerator> source,
            NativeLinqSelector<TSource, TResult> selector)
            where TSource : unmanaged
            where TResult : unmanaged
            where TEnumerator : unmanaged, IEnumerator<TSource>
        {
            return Throw<NativeDelegateQuery<TResult, NativeDelegateSelectQuery<TSource, TResult, TEnumerator>>>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TResult Sum<TSource, TResult, TEnumerator>(
            this NativeDelegateQuery<TSource, TEnumerator> source,
            NativeLinqSelector<TSource, TResult> selector)
            where TSource : unmanaged
            where TResult : unmanaged
            where TEnumerator : unmanaged, IEnumerator<TSource>
        {
            return Throw<TResult>();
        }

        private static T Throw<T>()
        {
            throw new InvalidOperationException("NativeLinq delegate query was not IL-woven.");
        }
    }
}
