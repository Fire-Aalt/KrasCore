using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            var list = ToNativeList(Allocator.Temp);
            return list.ToArray(allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray<T> ToUnsafeArray(Allocator allocator)
        {
            var list = ToNativeList(Allocator.Temp);
            var array = new UnsafeArray<T>(list.Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeArray<T>.Copy(list.AsArray(), array);
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeList<T> ToUnsafeList(AllocatorManager.AllocatorHandle allocator)
        {
            var enumerator = GetEnumerator();
            var list = new UnsafeList<T>(0, allocator);
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
            }

            enumerator.Dispose();
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            var enumerator = GetEnumerator();
            var list = new NativeList<T>(allocator);
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
            }

            enumerator.Dispose();
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToManagedArray()
        {
            var list = ToNativeList(Allocator.Temp);
            return list.ToManagedArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ToManagedList()
        {
            var enumerator = GetEnumerator();
            var list = new List<T>();
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
            }

            enumerator.Dispose();
            return list;
        }
    }
}
