using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<T, NativeArray<T>.Enumerator> AsQuery<T>(this NativeArray<T> collection)
            where T : unmanaged
        {
            return new Query<T, NativeArray<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<T, NativeArray<T>.Enumerator> AsQuery<T>(this NativeList<T> collection)
            where T : unmanaged
        {
            return new Query<T, NativeArray<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<T, UnsafeList<T>.Enumerator> AsQuery<T>(this UnsafeList<T> collection)
            where T : unmanaged
        {
            return new Query<T, UnsafeList<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<T, NativeQueue<T>.Enumerator> AsQuery<T>(this NativeQueue<T> collection)
            where T : unmanaged
        {
            return new Query<T, NativeQueue<T>.Enumerator>(collection.AsReadOnly().GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<T, NativeHashSet<T>.Enumerator> AsQuery<T>(this NativeHashSet<T> collection)
            where T : unmanaged, IEquatable<T>
        {
            return new Query<T, NativeHashSet<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<T, NativeParallelHashSet<T>.Enumerator> AsQuery<T>(this NativeParallelHashSet<T> collection)
            where T : unmanaged, IEquatable<T>
        {
            return new Query<T, NativeParallelHashSet<T>.Enumerator>(collection.GetEnumerator());
        }
    }
}
