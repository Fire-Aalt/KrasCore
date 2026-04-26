using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public partial struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFirst(out T value)
        {
            return NativeLinqUtilities.TryFirst<T, Enumerator>(GetEnumerator(), out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T First()
        {
            return NativeLinqUtilities.First<T, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstOrDefault()
        {
            return NativeLinqUtilities.FirstOrDefault<T, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstOrDefault<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<T>
        {
            return NativeLinqUtilities.FirstOrDefault<T, Enumerator, TPredicate>(GetEnumerator(), predicate);
        }
    }

    public partial struct NativeWhereEnumerable<T, TEnumerator, TPredicate>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFirst(out T value)
        {
            return NativeLinqUtilities.TryFirst<T, Enumerator>(GetEnumerator(), out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T First()
        {
            return NativeLinqUtilities.First<T, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstOrDefault()
        {
            return NativeLinqUtilities.FirstOrDefault<T, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstOrDefault<TNextPredicate>(TNextPredicate predicate)
            where TNextPredicate : unmanaged, IPredicate<T>
        {
            return NativeLinqUtilities.FirstOrDefault<T, Enumerator, TNextPredicate>(GetEnumerator(), predicate);
        }
    }

    public partial struct NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFirst(out TResult value)
        {
            return NativeLinqUtilities.TryFirst<TResult, Enumerator>(GetEnumerator(), out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult First()
        {
            return NativeLinqUtilities.First<TResult, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult FirstOrDefault()
        {
            return NativeLinqUtilities.FirstOrDefault<TResult, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult FirstOrDefault<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<TResult>
        {
            return NativeLinqUtilities.FirstOrDefault<TResult, Enumerator, TPredicate>(GetEnumerator(), predicate);
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
        public bool TryFirst(out TResult value)
        {
            return NativeLinqUtilities.TryFirst<TResult, Enumerator>(GetEnumerator(), out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult First()
        {
            return NativeLinqUtilities.First<TResult, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult FirstOrDefault()
        {
            return NativeLinqUtilities.FirstOrDefault<TResult, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult FirstOrDefault<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<TResult>
        {
            return NativeLinqUtilities.FirstOrDefault<TResult, Enumerator, TPredicate>(GetEnumerator(), predicate);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFirst<T, TEnumerator>(TEnumerator enumerator, out T value)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            if (enumerator.MoveNext())
            {
                value = enumerator.Current;
                enumerator.Dispose();
                return true;
            }

            value = default;
            enumerator.Dispose();
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T First<T, TEnumerator>(TEnumerator enumerator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            if (TryFirst<T, TEnumerator>(enumerator, out var value))
            {
                return value;
            }

            throw new InvalidOperationException("The NativeLinq source contains no elements.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstOrDefault<T, TEnumerator>(TEnumerator enumerator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return TryFirst<T, TEnumerator>(enumerator, out var value) ? value : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstOrDefault<T, TEnumerator, TPredicate>(TEnumerator enumerator, TPredicate predicate)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                if (predicate.Match(in value))
                {
                    enumerator.Dispose();
                    return value;
                }
            }

            enumerator.Dispose();
            return default;
        }
    }
}


