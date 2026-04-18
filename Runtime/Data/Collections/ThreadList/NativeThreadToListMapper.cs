using System;
using Unity.Collections;
using Unity.Jobs;

namespace KrasCore
{
    public struct NativeThreadToListMapper<T> : IDisposable where T : unmanaged
    {
        public NativeThreadList<T> NativeThreadList;
        public NativeList<T> List;

        public NativeThreadToListMapper(int capacity, Allocator allocator)
        {
            NativeThreadList = new NativeThreadList<T>(capacity, allocator);
            List = new NativeList<T>(capacity, allocator);
        }

        public void Clear()
        {
            NativeThreadList.Clear();
            List.Clear();
        }

        public JobHandle CopyParallelToListSingle(JobHandle dependency, UnsafeThreadList<T>.UnsafeParallelListToArraySingleThreaded jobStud = default)
        {
            return NativeThreadList.CopyToListSingle(ref List, dependency, jobStud);
        }

        public void CopyParallelToList()
        {
            NativeThreadList.CopyToList(ref List);
        }
        
        public NativeThreadList<T>.ThreadWriter AsThreadWriter() => NativeThreadList.AsThreadWriter();
        public NativeThreadList<T>.ThreadReader AsThreadReader() => NativeThreadList.AsThreadReader();
        
        public void Dispose()
        {
            NativeThreadList.Dispose();
            List.Dispose();
        }
    }
}