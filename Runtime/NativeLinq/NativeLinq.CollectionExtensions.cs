using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, NativeArray<T>.Enumerator> AsNativeEnumerable<T>(this NativeArray<T> collection)
            where T : unmanaged
        {
            return new NativeEnumerable<T, NativeArray<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, NativeArray<T>.Enumerator> AsNativeEnumerable<T>(this NativeList<T> collection)
            where T : unmanaged
        {
            return new NativeEnumerable<T, NativeArray<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, UnsafeList<T>.Enumerator> AsNativeEnumerable<T>(this UnsafeList<T> collection)
            where T : unmanaged
        {
            return new NativeEnumerable<T, UnsafeList<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, NativeQueue<T>.Enumerator> AsNativeEnumerable<T>(this NativeQueue<T> collection)
            where T : unmanaged
        {
            return new NativeEnumerable<T, NativeQueue<T>.Enumerator>(collection.AsReadOnly().GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, NativeHashSet<T>.Enumerator> AsNativeEnumerable<T>(this NativeHashSet<T> collection)
            where T : unmanaged, IEquatable<T>
        {
            return new NativeEnumerable<T, NativeHashSet<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, NativeParallelHashSet<T>.Enumerator> AsNativeEnumerable<T>(this NativeParallelHashSet<T> collection)
            where T : unmanaged, IEquatable<T>
        {
            return new NativeEnumerable<T, NativeParallelHashSet<T>.Enumerator>(collection.GetEnumerator());
        }
    }
}


