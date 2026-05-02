using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFirst(out T value)
        {
            var enumerator = GetEnumerator();
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
        public T First()
        {
            if (TryFirst(out var value))
            {
                return value;
            }

            throw new InvalidOperationException("The NativeLinq source contains no elements.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstOrDefault()
        {
            return TryFirst(out var value) ? value : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstOrDefault<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<T>
        {
            var enumerator = GetEnumerator();
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
    
    // public partial class NativeLinqExtensions
    // {
    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     public static T FirstOrDefault<T, TEnumerator, TPredicate>(this Query<T, TEnumerator> source, TPredicate predicate)
    //         where T : unmanaged
    //         where TEnumerator : unmanaged, IEnumerator<T>
    //         where TPredicate : unmanaged, IPredicate<T>
    //     {
    //         var enumerator = source.GetEnumerator();
    //         while (enumerator.MoveNext())
    //         {
    //             var value = enumerator.Current;
    //             if (predicate.Match(in value))
    //             {
    //                 enumerator.Dispose();
    //                 return value;
    //             }
    //         }
    //
    //         enumerator.Dispose();
    //         return default;
    //     }
    // }
}
