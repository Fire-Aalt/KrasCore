using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace KrasCore
{
    public struct UnsafeParallelToListMapper<T> : IDisposable where T : unmanaged
    {
        public UnsafeParallelList<T> ParallelList;
        public UnsafeList<T> List;

        public UnsafeParallelToListMapper(int capacity, Allocator allocator)
        {
            ParallelList = new UnsafeParallelList<T>(capacity, allocator);
            List = new UnsafeList<T>(capacity, allocator);
        }

        public void Clear()
        {
            ParallelList.Clear();
            List.Clear();
        }

        public unsafe JobHandle CopyParallelToListSingle(JobHandle dependency, UnsafeParallelList<T>.UnsafeParallelListToArraySingleThreaded jobStud = default)
        {
            var list = (UnsafeList<T>*)UnsafeUtility.AddressOf(ref List);
            return ParallelList.CopyToListSingle(list, dependency, jobStud);
        }

        public UnsafeParallelList<T>.ThreadWriter AsThreadWriter() => ParallelList.AsThreadWriter();
        public UnsafeParallelList<T>.ThreadReader AsThreadReader() => ParallelList.AsThreadReader();
        
        public void Dispose()
        {
            ParallelList.Dispose();
            List.Dispose();
        }
    }
}
