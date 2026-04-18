using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace KrasCore
{
    public struct UnsafeThreadToListMapper<T> : IDisposable where T : unmanaged
    {
        public UnsafeThreadList<T> ThreadList;
        public UnsafeList<T> List;

        public bool IsCreated => ThreadList.IsCreated && List.IsCreated;
        
        public UnsafeThreadToListMapper(int capacity, Allocator allocator)
        {
            ThreadList = new UnsafeThreadList<T>(capacity, allocator);
            List = new UnsafeList<T>(capacity, allocator);
        }

        public void Clear()
        {
            ThreadList.Clear();
            List.Clear();
        }

        public unsafe JobHandle CopyParallelToListSingle(JobHandle dependency, UnsafeThreadList<T>.UnsafeParallelListToArraySingleThreaded jobStud = default)
        {
            var list = (UnsafeList<T>*)UnsafeUtility.AddressOf(ref List);
            return ThreadList.CopyToListSingle(list, dependency, jobStud);
        }

        public unsafe void CopyParallelToList()
        {
            var list = (UnsafeList<T>*)UnsafeUtility.AddressOf(ref List);
            ThreadList.CopyToList(list);
        }
        
        public UnsafeThreadList<T>.ThreadWriter AsThreadWriter() => ThreadList.AsThreadWriter();
        public UnsafeThreadList<T>.ThreadReader AsThreadReader() => ThreadList.AsThreadReader();
        
        public void Dispose()
        {
            ThreadList.Dispose();
            List.Dispose();
        }
    }
}
