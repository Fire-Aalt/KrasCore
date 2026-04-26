using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public partial struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            return NativeLinqUtilities.ToNativeList<T, Enumerator>(GetEnumerator(), allocator);
        }
    }

    public partial struct NativeWhereEnumerable<T, TEnumerator, TPredicate>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            return NativeLinqUtilities.ToNativeList<T, Enumerator>(GetEnumerator(), allocator);
        }
    }

    public partial struct NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<TResult> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            return NativeLinqUtilities.ToNativeList<TResult, Enumerator>(GetEnumerator(), allocator);
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
        public NativeList<TResult> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            return NativeLinqUtilities.ToNativeList<TResult, Enumerator>(GetEnumerator(), allocator);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> ToNativeList<T, TEnumerator>(TEnumerator enumerator, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            var list = new NativeList<T>(allocator);
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
            }

            enumerator.Dispose();
            return list;
        }
    }
}


